using Application.Common;
using MediatR;

namespace Application.Features.Customer.Commands.CreateCustomer;

public record CreateCustomerCommand(
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string PhoneNumber,
    string Email,
    string BankAccountNumber
) : IRequest<Result<Guid>>;