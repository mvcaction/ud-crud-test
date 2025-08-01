
namespace Infrastructure.Resilience;

public interface IResilienceService
{
    /// <summary>
    /// Executes an async operation with retry policy
    /// </summary>
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName);
    
    /// <summary>
    /// Executes an async operation with circuit breaker
    /// </summary>
    Task<T> ExecuteWithCircuitBreakerAsync<T>(Func<Task<T>> operation, string operationName);
    
    /// <summary>
    /// Executes an async operation with combined retry and circuit breaker policies
    /// </summary>
    Task<T> ExecuteWithResilienceAsync<T>(Func<Task<T>> operation, string operationName);
    
    /// <summary>
    /// Gets a configured HttpClient with resilience policies
    /// </summary>
    HttpClient GetResilientHttpClient(string clientName = "default");
}
