using Domain.SeedWork.Primitives;
using System.Text.RegularExpressions;

namespace Domain.Aggregates.Customer.ValueObjects;

public sealed class PhoneNumber : ValueObject
{
    private static readonly Regex PhoneRegex = new(
        @"^\+[1-9]\d{1,14}$",
        RegexOptions.Compiled);

    public string CountryCode { get; private set; }
    public string Number { get; private set; }
    public string FullNumber => $"{CountryCode}{Number}";

    private PhoneNumber(string countryCode, string number)
    {
        CountryCode = countryCode;
        Number = number;
    }

    public static PhoneNumber Create(string fullNumber)
    {
        if (string.IsNullOrWhiteSpace(fullNumber))
            throw new ArgumentException("Phone number cannot be empty", nameof(fullNumber));

        // Clean the input
        var cleanNumber = fullNumber.Trim().Replace(" ", "").Replace("-", "");

        if (!PhoneRegex.IsMatch(cleanNumber))
            throw new ArgumentException("Invalid phone number format. Use international format (+1234567890)", nameof(fullNumber));

        // Extract country code (first 1-4 digits after +)
        var countryCode = cleanNumber.Substring(0, Math.Min(5, cleanNumber.Length));
        for (int i = 2; i <= 5 && i <= cleanNumber.Length; i++)
        {
            if (IsValidCountryCode(cleanNumber.Substring(0, i)))
            {
                countryCode = cleanNumber.Substring(0, i);
                break;
            }
        }

        var number = cleanNumber.Substring(countryCode.Length);

        return new PhoneNumber(countryCode, number);
    }

    private static bool IsValidCountryCode(string code)
    {
        // Simplified validation - in real world, use a comprehensive list
        return code.Length >= 2 && code.Length <= 4 && code.StartsWith("+");
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return FullNumber;
    }

    public static implicit operator string(PhoneNumber phoneNumber) => phoneNumber.FullNumber;
    public static explicit operator PhoneNumber(string value) => Create(value);
}