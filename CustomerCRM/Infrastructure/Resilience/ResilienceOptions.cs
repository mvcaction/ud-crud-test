namespace Infrastructure.Resilience;

public class ResilienceOptions
{
    public const string SectionName = "Resilience";
    
    public RetryPolicyOptions RetryPolicy { get; set; } = new();
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
    public TimeoutOptions Timeout { get; set; } = new();
}

public class RetryPolicyOptions
{
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
    public bool UseJitter { get; set; } = true;
}

public class CircuitBreakerOptions
{
    public int HandledEventsAllowedBeforeBreaking { get; set; } = 5;
    public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromSeconds(30);
    public int SamplingDurationInSeconds { get; set; } = 60;
    public int MinimumThroughput { get; set; } = 10;
    public double FailureRatio { get; set; } = 0.5;
}

public class TimeoutOptions
{
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan LongRunningTimeout { get; set; } = TimeSpan.FromMinutes(2);
}