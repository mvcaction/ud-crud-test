using Application.Common;
using MediatR;

namespace Application.Features.Customer.Commands.CreateCustomer;

/// <summary>
/// Command to create a new customer in the system.
/// </summary>
/// <param name="FirstName">The customer's first name.</param>
/// <param name="LastName">The customer's last name.</param>
/// <param name="DateOfBirth">The customer's date of birth.</param>
/// <param name="PhoneNumber">The customer's phone number.</param>
/// <param name="Email">The customer's email address.</param>
/// <param name="BankAccountNumber">The customer's bank account number.</param>
public record CreateCustomerCommand(
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string PhoneNumber,
    string Email,
    string BankAccountNumber
) : IRequest<Result<Guid>>;