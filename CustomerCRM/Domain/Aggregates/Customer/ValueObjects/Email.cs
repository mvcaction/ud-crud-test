using Domain.SeedWork.Primitives;
using System.Text.RegularExpressions;

namespace Domain.Aggregates.Customer.ValueObjects;

public sealed class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; private set; }

    private Email()
    {
        Value = null!;   
    }
    private Email(string value)
    {
        Value = value;
    }
    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        if (value.Length > 100)
            throw new ArgumentException("Email cannot exceed 100 characters", nameof(value));

        if (!EmailRegex.IsMatch(value))
            throw new ArgumentException("Invalid email format", nameof(value));

        return new Email(value.ToLowerInvariant().Trim());
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(Email email) => email.Value;
    public static explicit operator Email(string value) => Create(value);
}