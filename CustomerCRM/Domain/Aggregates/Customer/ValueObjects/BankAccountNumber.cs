using Domain.SeedWork.Primitives;
using System.Text.RegularExpressions;

namespace Domain.Aggregates.Customer.ValueObjects;

public sealed class BankAccountNumber : ValueObject
{
    private static readonly Regex AccountRegex = new(
        @"^[A-Z]{2}\d{2}[A-Z0-9]{4,30}$",
        RegexOptions.Compiled);

    public string Value { get; private set; }

    private BankAccountNumber(string value)
    {
        Value = value;
    }

    public static BankAccountNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Bank account number cannot be empty", nameof(value));

        var cleanValue = value.Trim().Replace(" ", "").ToUpperInvariant();

        if (!AccountRegex.IsMatch(cleanValue))
            throw new ArgumentException("Invalid bank account number format (IBAN format required)", nameof(value));

        // Basic IBAN validation
        if (!IsValidIban(cleanValue))
            throw new ArgumentException("Invalid IBAN checksum", nameof(value));

        return new BankAccountNumber(cleanValue);
    }

    private static bool IsValidIban(string iban)
    {
        // Simplified IBAN validation - move first 4 chars to end
        var rearranged = iban.Substring(4) + iban.Substring(0, 4);

        // Convert letters to numbers (A=10, B=11, etc.)
        var numericString = "";
        foreach (char c in rearranged)
        {
            if (char.IsDigit(c))
                numericString += c;
            else if (char.IsLetter(c))
                numericString += (char.ToUpperInvariant(c) - 'A' + 10).ToString();
        }

        // Check if remainder when divided by 97 is 1
        return ModuloLargeNumber(numericString, 97) == 1;
    }

    private static int ModuloLargeNumber(string number, int modulus)
    {
        int remainder = 0;
        foreach (char digit in number)
        {
            remainder = (remainder * 10 + (digit - '0')) % modulus;
        }
        return remainder;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(BankAccountNumber accountNumber) => accountNumber.Value;
    public static explicit operator BankAccountNumber(string value) => Create(value);
}