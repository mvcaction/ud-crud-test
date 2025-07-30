using Domain.SeedWork.Primitives;

namespace Domain.Aggregates.Customer.ValueObjects;

public sealed class FirstName : ValueObject
{
    public string Value { get; private set; } = string.Empty;  

    private FirstName()
    {
        Value = string.Empty; // یا default!
    }

    private FirstName(string value)
    {
        Value = value;
    }

    public static FirstName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("First name cannot be empty", nameof(value));

        if (value.Length > 50)
            throw new ArgumentException("First name cannot exceed 50 characters", nameof(value));

        if (value.Length < 2)
            throw new ArgumentException("First name must be at least 2 characters", nameof(value));

        return new FirstName(value.Trim());
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(FirstName firstName) => firstName.Value;
    public static explicit operator FirstName(string value) => Create(value);
}