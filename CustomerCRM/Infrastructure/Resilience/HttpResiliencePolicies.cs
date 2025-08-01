using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using System.Net;

namespace Infrastructure.Resilience;

public static class HttpResiliencePolicies
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(
        ResilienceOptions options,
        ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .OrResult(msg => 
                msg.StatusCode == HttpStatusCode.TooManyRequests ||
                msg.StatusCode >= HttpStatusCode.InternalServerError)
            .WaitAndRetryAsync(
                retryCount: options.RetryPolicy.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt =>
                {
                    var delay = TimeSpan.FromMilliseconds(
                        Math.Min(
                            options.RetryPolicy.BaseDelay.TotalMilliseconds * Math.Pow(2, retryAttempt - 1),
                            options.RetryPolicy.MaxDelay.TotalMilliseconds));
                    
                    if (options.RetryPolicy.UseJitter)
                    {
                        var jitter = TimeSpan.FromMilliseconds(
                            Random.Shared.Next(0, (int)(delay.TotalMilliseconds * 0.1)));
                        delay = delay.Add(jitter);
                    }
                    
                    return delay;
                },
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    logger.LogWarning(
                        "HTTP Retry {RetryCount}/{MaxRetries} in {Delay}ms. Status: {StatusCode}, Reason: {Reason}",
                        retryCount, options.RetryPolicy.MaxRetryAttempts, timespan.TotalMilliseconds,
                        outcome.Result?.StatusCode, outcome.Result?.ReasonPhrase ?? outcome.Exception?.Message);
                });
    }

    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(
        ResilienceOptions options,
        ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .OrResult(msg => msg.StatusCode >= HttpStatusCode.InternalServerError)
            .AdvancedCircuitBreakerAsync(
                failureThreshold: options.CircuitBreaker.FailureRatio,
                samplingDuration: TimeSpan.FromSeconds(options.CircuitBreaker.SamplingDurationInSeconds),
                minimumThroughput: options.CircuitBreaker.MinimumThroughput,
                durationOfBreak: options.CircuitBreaker.DurationOfBreak,
                onBreak: (result, duration) =>
                {
                    logger.LogWarning(
                        "HTTP Circuit breaker opened for {Duration}s. Last failure: {StatusCode} {Reason}",
                        duration.TotalSeconds,
                        result.Result?.StatusCode ?? HttpStatusCode.InternalServerError,
                        result.Result?.ReasonPhrase ?? result.Exception?.Message);
                },
                onReset: () => logger.LogInformation("HTTP Circuit breaker reset"),
                onHalfOpen: () => logger.LogInformation("HTTP Circuit breaker half-open"));
    }

    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(ResilienceOptions options)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(options.Timeout.DefaultTimeout);
    }

    public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy(
        ResilienceOptions options,
        ILogger logger)
    {
        var retryPolicy = GetRetryPolicy(options, logger);
        var circuitBreakerPolicy = GetCircuitBreakerPolicy(options, logger);
        var timeoutPolicy = GetTimeoutPolicy(options);

        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
    }
}