using Domain.SeedWork.Primitives;
using Domain.Aggregates.Customer.Services;
using Domain.Aggregates.Customer.ValueObjects;

namespace Domain.Aggregates.Customer.Rules;

public class CustomerEmailMustBeUniqueRule : IBusinessRule
{
    private readonly Email _email;
    private readonly Guid? _customerId;
    private readonly ICustomerUniquenessCheckerService _uniquenessChecker;

    public CustomerEmailMustBeUniqueRule(
        Email email,     
        ICustomerUniquenessCheckerService uniquenessChecker,
        Guid? customerId = null)
    {
        _email = email;
        _customerId = customerId;
        _uniquenessChecker = uniquenessChecker;
    }

    public bool IsBroken()
    {
        // Use Task.Run to execute the async method on a background thread
        // This prevents deadlocks while maintaining the synchronous interface
        return Task.Run(async () => await _uniquenessChecker.IsEmailTaken(_email.Value, _customerId))
            .GetAwaiter()
            .GetResult();
    }

    public string Message => $"Customer email '{_email.Value}' must be unique.";
}