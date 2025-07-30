namespace Domain.Aggregates.Customer.Services;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}