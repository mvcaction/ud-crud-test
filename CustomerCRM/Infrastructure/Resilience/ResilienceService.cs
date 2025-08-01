using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Net.Sockets;

namespace Infrastructure.Resilience;

internal class ResilienceService : IResilienceService
{
    private readonly ILogger<ResilienceService> _logger;
    private readonly ResilienceOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    
    private readonly ResiliencePipeline _retryPipeline;
    private readonly ResiliencePipeline _circuitBreakerPipeline;
    private readonly ResiliencePipeline _combinedPipeline;

    public ResilienceService(
        ILogger<ResilienceService> logger,
        IOptions<ResilienceOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        
        _retryPipeline = CreateRetryPipeline();
        _circuitBreakerPipeline = CreateCircuitBreakerPipeline();
        _combinedPipeline = CreateCombinedPipeline();
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName)
    {
        return await _retryPipeline.ExecuteAsync(async (context) =>
        {
            _logger.LogDebug("Executing operation: {OperationName}", operationName);
            return await operation();
        });
    }

    public async Task<T> ExecuteWithCircuitBreakerAsync<T>(Func<Task<T>> operation, string operationName)
    {
        try
        {
            return await _circuitBreakerPipeline.ExecuteAsync(async (context) =>
            {
                _logger.LogDebug("Executing operation with circuit breaker: {OperationName}", operationName);
                return await operation();
            });
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuit breaker is open for operation: {OperationName}", operationName);
            throw;
        }
    }

    public async Task<T> ExecuteWithResilienceAsync<T>(Func<Task<T>> operation, string operationName)
    {
        return await _combinedPipeline.ExecuteAsync(async (context) =>
        {
            _logger.LogDebug("Executing operation with full resilience: {OperationName}", operationName);
            return await operation();
        });
    }

    public HttpClient GetResilientHttpClient(string clientName = "default")
    {
        return _httpClientFactory.CreateClient(clientName);
    }

    private ResiliencePipeline CreateRetryPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutRejectedException>()
                    .Handle<SocketException>()
                    .Handle<OperationCanceledException>(),
                MaxRetryAttempts = _options.RetryPolicy.MaxRetryAttempts,
                DelayGenerator = static args =>
                {
                    var delay = TimeSpan.FromMilliseconds(
                        Math.Min(
                            1000 * Math.Pow(2, args.AttemptNumber - 1), // Exponential backoff: 1s, 2s, 4s, 8s...
                            30000)); // Max 30 seconds
                    
                    // Add jitter (10% randomization)
                    var jitter = TimeSpan.FromMilliseconds(
                        Random.Shared.Next(0, (int)(delay.TotalMilliseconds * 0.1)));
                    
                    return ValueTask.FromResult<TimeSpan?>(delay.Add(jitter));
                },
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Retry {AttemptNumber}/{MaxRetries} after {Delay}ms. Exception: {Exception}",
                        args.AttemptNumber, _options.RetryPolicy.MaxRetryAttempts, 
                        args.RetryDelay.TotalMilliseconds, args.Outcome.Exception?.Message ?? "Unknown");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    private ResiliencePipeline CreateCircuitBreakerPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutRejectedException>()
                    .Handle<SocketException>()
                    .Handle<OperationCanceledException>(),
                FailureRatio = _options.CircuitBreaker.FailureRatio,
                SamplingDuration = TimeSpan.FromSeconds(_options.CircuitBreaker.SamplingDurationInSeconds),
                MinimumThroughput = _options.CircuitBreaker.MinimumThroughput,
                BreakDuration = _options.CircuitBreaker.DurationOfBreak,
                OnOpened = args =>
                {
                    _logger.LogWarning(
                        "Circuit breaker opened for {Duration}s. Exception: {Exception}",
                        args.BreakDuration.TotalSeconds, args.Outcome.Exception?.Message ?? "Unknown");
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Circuit breaker reset - normal operation restored");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation("Circuit breaker half-open - testing if service has recovered");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    private ResiliencePipeline CreateCombinedPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutRejectedException>()
                    .Handle<SocketException>()
                    .Handle<OperationCanceledException>(),
                MaxRetryAttempts = _options.RetryPolicy.MaxRetryAttempts,
                DelayGenerator = args =>
                {
                    // Use configuration-based delays with exponential backoff
                    var baseDelayMs = _options.RetryPolicy.BaseDelay.TotalMilliseconds;
                    var maxDelayMs = _options.RetryPolicy.MaxDelay.TotalMilliseconds;
                    
                    var delay = TimeSpan.FromMilliseconds(
                        Math.Min(baseDelayMs * Math.Pow(2, args.AttemptNumber - 1), maxDelayMs));
                    
                    if (_options.RetryPolicy.UseJitter)
                    {
                        var jitter = TimeSpan.FromMilliseconds(
                            Random.Shared.Next(0, (int)(delay.TotalMilliseconds * 0.1)));
                        delay = delay.Add(jitter);
                    }
                    
                    return ValueTask.FromResult<TimeSpan?>(delay);
                },
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Combined Retry {AttemptNumber}/{MaxRetries} after {Delay}ms",
                        args.AttemptNumber, _options.RetryPolicy.MaxRetryAttempts, 
                        args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutRejectedException>()
                    .Handle<SocketException>()
                    .Handle<OperationCanceledException>(),
                FailureRatio = _options.CircuitBreaker.FailureRatio,
                SamplingDuration = TimeSpan.FromSeconds(_options.CircuitBreaker.SamplingDurationInSeconds),
                MinimumThroughput = _options.CircuitBreaker.MinimumThroughput,
                BreakDuration = _options.CircuitBreaker.DurationOfBreak,
                OnOpened = args =>
                {
                    _logger.LogWarning("Combined Circuit breaker opened for {Duration}s", 
                        args.BreakDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(_options.Timeout.DefaultTimeout)
            .Build();
    }
}