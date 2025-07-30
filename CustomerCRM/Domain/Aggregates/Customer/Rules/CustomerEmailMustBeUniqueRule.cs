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
        return _uniquenessChecker.IsEmailTaken(_email.Value, _customerId)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    public string Message => $"Customer email '{_email.Value}' must be unique.";
}