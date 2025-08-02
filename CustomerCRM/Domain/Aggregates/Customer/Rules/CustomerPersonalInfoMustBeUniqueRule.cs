using Domain.SeedWork.Primitives;
using Domain.Aggregates.Customer.Services;
using Domain.Aggregates.Customer.ValueObjects;

namespace Domain.Aggregates.Customer.Rules;

/// <summary>
/// Business rule that ensures customer personal information (first name, last name, and date of birth) is unique across the system.
/// </summary>
/// <remarks>
/// This rule prevents duplicate customers from being created or updated with identical personal information.
/// The rule can optionally exclude a specific customer ID when validating during updates.
/// </remarks>
public class CustomerPersonalInfoMustBeUniqueRule : IBusinessRule
{
    private readonly FirstName _firstName;
    private readonly LastName _lastName;
    private readonly DateOfBirth _dateOfBirth;
    private readonly Guid? _customerId;
    private readonly ICustomerUniquenessCheckerService _uniquenessChecker;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerPersonalInfoMustBeUniqueRule"/> class.
    /// </summary>
    /// <param name="firstName">The customer's first name to validate for uniqueness.</param>
    /// <param name="lastName">The customer's last name to validate for uniqueness.</param>
    /// <param name="dateOfBirth">The customer's date of birth to validate for uniqueness.</param>
    /// <param name="uniquenessChecker">Service to check customer uniqueness against the data store.</param>
    /// <param name="customerId">Optional customer ID to exclude from uniqueness check (used during updates).</param>
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

    /// <summary>
    /// Determines whether the business rule is broken by checking if another customer 
    /// with the same personal information already exists.
    /// </summary>
    /// <returns>
    /// <c>true</c> if a customer with identical personal information already exists; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Uses Task.Run to execute the async method on a background thread to prevent deadlocks 
    /// while maintaining the synchronous interface required by <see cref="IBusinessRule"/>.
    /// </remarks>
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

    /// <summary>
    /// Gets the error message that describes the business rule violation.
    /// </summary>
    /// <value>
    /// A formatted message indicating which customer personal information already exists.
    /// </value>
    public string Message => $"Customer with personal information '{_firstName.Value} {_lastName.Value}' and date of birth '{_dateOfBirth.Value:yyyy-MM-dd}' already exists.";
}