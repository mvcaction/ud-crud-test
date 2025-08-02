using Domain.SeedWork.Primitives;
using Domain.Aggregates.Customer.Services;
using Domain.Aggregates.Customer.ValueObjects;

namespace Domain.Aggregates.Customer.Rules;

public class CustomerPersonalInfoMustBeUniqueRule : IBusinessRule
{
    private readonly FirstName _firstName;
    private readonly LastName _lastName;
    private readonly DateOfBirth _dateOfBirth;
    private readonly Guid? _customerId;
    private readonly ICustomerUniquenessCheckerService _uniquenessChecker;

    public CustomerPersonalInfoMustBeUniqueRule(
        FirstName firstName,        
        LastName lastName,          
        DateOfBirth dateOfBirth,    
        ICustomerUniquenessCheckerService uniquenessChecker,
        Guid? customerId = null)
    {
        _firstName = firstName;
        _lastName = lastName;
        _dateOfBirth = dateOfBirth;
        _customerId = customerId;
        _uniquenessChecker = uniquenessChecker;
    }

    public bool IsBroken()
    {
        // Use Task.Run to execute the async method on a background thread
        // This prevents deadlocks while maintaining the synchronous interface
        return Task.Run(async () => await _uniquenessChecker.IsPersonalInfoTaken(
            _firstName.Value,
            _lastName.Value,
            _dateOfBirth.Value,
            _customerId))
            .GetAwaiter()
            .GetResult();
    }

    public string Message => $"Customer with personal information '{_firstName.Value} {_lastName.Value}' and date of birth '{_dateOfBirth.Value:yyyy-MM-dd}' already exists.";
}