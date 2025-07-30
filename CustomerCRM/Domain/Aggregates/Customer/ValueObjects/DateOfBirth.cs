using Domain.SeedWork.Primitives;

namespace Domain.Aggregates.Customer.ValueObjects;

public sealed class DateOfBirth : ValueObject
{
    public DateTime Value { get; private set; }

    private DateOfBirth(DateTime value)
    {
        Value = value;
    }

    public static DateOfBirth Create(DateTime value)
    {
        if (value > DateTime.Today)
            throw new ArgumentException("Date of birth cannot be in the future", nameof(value));

        var age = DateTime.Today.Year - value.Year;
        if (value.Date > DateTime.Today.AddYears(-age))
            age--;

        if (age < 18)
            throw new ArgumentException("Customer must be at least 18 years old", nameof(value));

        if (age > 120)
            throw new ArgumentException("Invalid date of birth", nameof(value));

        return new DateOfBirth(value.Date);
    }

    public int CalculateAge()
    {
        var age = DateTime.Today.Year - Value.Year;
        if (Value.Date > DateTime.Today.AddYears(-age))
            age--;
        return age;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator DateTime(DateOfBirth dateOfBirth) => dateOfBirth.Value;
    public static explicit operator DateOfBirth(DateTime value) => Create(value);
}