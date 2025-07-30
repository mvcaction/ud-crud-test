using Domain.Aggregates.Customer.Events;
using Domain.Aggregates.Customer.Rules;
using Domain.Aggregates.Customer.Services;
using Domain.SeedWork.Primitives;
using Domain.Aggregates.Customer.ValueObjects;
using Domain.SeedWork.Exceptions;

namespace Domain.Aggregates.Customer;

public sealed class Customer : AggregateRoot<Guid>, ISoftDelete
{
    public FirstName FirstName { get; private set; } = null!;
    public LastName LastName { get; private set; } = null!;
    public DateOfBirth DateOfBirth { get; private set; } = null!;
    public PhoneNumber PhoneNumber { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public BankAccountNumber BankAccountNumber { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private readonly IDateTimeProvider _dateTimeProvider = null!;

    // EF Core Constructor
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private Customer() : base()
    {
        // EF Core will populate these values through reflection
    }
#pragma warning restore CS8618

    private Customer(
        Guid id,
        FirstName firstName,
        LastName lastName,
        DateOfBirth dateOfBirth,
        PhoneNumber phoneNumber,
        Email email,
        BankAccountNumber bankAccountNumber,
        IDateTimeProvider dateTimeProvider) : base(id)
    {
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        DateOfBirth = dateOfBirth ?? throw new ArgumentNullException(nameof(dateOfBirth));
        PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        BankAccountNumber = bankAccountNumber ?? throw new ArgumentNullException(nameof(bankAccountNumber));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        CreatedAt = _dateTimeProvider.UtcNow;
        IsDeleted = false;
    }

    public static Customer Create(
        FirstName firstName,
        LastName lastName,
        DateOfBirth dateOfBirth,
        PhoneNumber phoneNumber,
        Email email,
        BankAccountNumber bankAccountNumber,
        ICustomerUniquenessCheckerService customerUniquenessCheckerService,
        IDateTimeProvider dateTimeProvider)
    {
        var customerId = Guid.NewGuid();

        CheckRule(new CustomerEmailMustBeUniqueRule(email, customerUniquenessCheckerService));
        CheckRule(new CustomerPersonalInfoMustBeUniqueRule(firstName, lastName, dateOfBirth, customerUniquenessCheckerService));

        var customer = new Customer(
            customerId,
            firstName,
            lastName,
            dateOfBirth,
            phoneNumber,
            email,
            bankAccountNumber,
            dateTimeProvider);

        customer.RaiseDomainEvent(new CustomerCreatedDomainEvent(
            customerId,
            email,
            firstName,
            lastName,
            customer.CreatedAt));

        return customer;
    }

    public void ChangeFirstName(
        FirstName firstName,
        ICustomerUniquenessCheckerService customerUniquenessCheckerService)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot modify deleted customer");

        CheckRule(new CustomerPersonalInfoMustBeUniqueRule(firstName, LastName, DateOfBirth, customerUniquenessCheckerService, Id));

        FirstName = firstName;
        UpdatedAt = _dateTimeProvider.UtcNow;

        RaiseDomainEvent(new CustomerUpdatedDomainEvent(Id, Email, FirstName, LastName, UpdatedAt.Value));
    }

    public void ChangeLastName(
        LastName lastName,
        ICustomerUniquenessCheckerService customerUniquenessCheckerService)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot modify deleted customer");

        CheckRule(new CustomerPersonalInfoMustBeUniqueRule(FirstName, lastName, DateOfBirth, customerUniquenessCheckerService, Id));

        LastName = lastName;
        UpdatedAt = _dateTimeProvider.UtcNow;

        RaiseDomainEvent(new CustomerUpdatedDomainEvent(Id, Email, FirstName, LastName, UpdatedAt.Value));
    }

    public void ChangeEmail(
        Email email,
        ICustomerUniquenessCheckerService customerUniquenessCheckerService)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot modify deleted customer");

        CheckRule(new CustomerEmailMustBeUniqueRule(email, customerUniquenessCheckerService, Id));

        Email = email;
        UpdatedAt = _dateTimeProvider.UtcNow;

        RaiseDomainEvent(new CustomerUpdatedDomainEvent(Id, Email, FirstName, LastName, UpdatedAt.Value));
    }

    public void ChangePhoneNumber(PhoneNumber phoneNumber)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot modify deleted customer");

        PhoneNumber = phoneNumber;
        UpdatedAt = _dateTimeProvider.UtcNow;

        RaiseDomainEvent(new CustomerUpdatedDomainEvent(Id, Email, FirstName, LastName, UpdatedAt.Value));
    }

    public void ChangeDateOfBirth(
        DateOfBirth dateOfBirth,
        ICustomerUniquenessCheckerService customerUniquenessCheckerService)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot modify deleted customer");

        CheckRule(new CustomerPersonalInfoMustBeUniqueRule(FirstName, LastName, dateOfBirth, customerUniquenessCheckerService, Id));

        DateOfBirth = dateOfBirth;
        UpdatedAt = _dateTimeProvider.UtcNow;

        RaiseDomainEvent(new CustomerUpdatedDomainEvent(Id, Email, FirstName, LastName, UpdatedAt.Value));
    }

    public void ChangeBankAccountNumber(BankAccountNumber bankAccountNumber)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Cannot modify deleted customer");

        BankAccountNumber = bankAccountNumber;
        UpdatedAt = _dateTimeProvider.UtcNow;

        RaiseDomainEvent(new CustomerUpdatedDomainEvent(Id, Email, FirstName, LastName, UpdatedAt.Value));
    }

    public void Delete()
    {
        if (IsDeleted)
            throw new InvalidOperationException("Customer is already deleted");

        IsDeleted = true;
        DeletedAt = _dateTimeProvider.UtcNow;

        RaiseDomainEvent(new CustomerDeletedDomainEvent(Id, Email, DeletedAt.Value));
    }

    public void Restore()
    {
        if (!IsDeleted)
            throw new InvalidOperationException("Customer is not deleted");

        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = _dateTimeProvider.UtcNow;

        RaiseDomainEvent(new CustomerRestoredDomainEvent(Id, Email, UpdatedAt.Value));
    }

    private static void CheckRule(IBusinessRule rule)
    {
        if (rule.IsBroken())
            throw new BusinessRuleValidationException(rule);
    }

    internal void SetDateTimeProvider(IDateTimeProvider dateTimeProvider)
    {
        var field = typeof(Customer)
            .GetField("_dateTimeProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(this, dateTimeProvider);
    }
}