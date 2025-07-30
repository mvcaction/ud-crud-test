using Domain.SeedWork.Primitives;

namespace Domain.Aggregates.Customer.ValueObjects;

public sealed class LastName : ValueObject
{
    public string Value { get; private set; }

    private LastName()
    {
        Value = null!;
    }

    private LastName(string value)
    {
        Value = value;
    }

    public static LastName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Last name cannot be empty", nameof(value));

        if (value.Length > 50)
            throw new ArgumentException("Last name cannot exceed 50 characters", nameof(value));

        if (value.Length < 2)
            throw new ArgumentException("Last name must be at least 2 characters", nameof(value));

        return new LastName(value.Trim());
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(LastName lastName) => lastName.Value;
    public static explicit operator LastName(string value) => Create(value);
}