using Application.Common;
using Application.Features.Customer.Abstractions;
using Domain.Aggregates.Customer.Services;
using Domain.Aggregates.Customer.ValueObjects;
using Domain.SeedWork.Exceptions;
using MediatR;

namespace Application.Features.Customer.Commands.UpdateCustomer;

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, Result>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerUniquenessCheckerService _uniquenessChecker;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        ICustomerUniquenessCheckerService uniquenessChecker,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _uniquenessChecker = uniquenessChecker;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken);
            if (customer == null)
                return Result.Failure("Customer not found");

            var firstName = FirstName.Create(request.FirstName);
            var lastName = LastName.Create(request.LastName);
            var dateOfBirth = DateOfBirth.Create(request.DateOfBirth);
            var phoneNumber = PhoneNumber.Create(request.PhoneNumber);
            var email = Email.Create(request.Email);
            var bankAccountNumber = BankAccountNumber.Create(request.BankAccountNumber);

            // Update each property individually to trigger proper domain events
            if (!customer.FirstName.Equals(firstName))
                customer.ChangeFirstName(firstName, _uniquenessChecker);

            if (!customer.LastName.Equals(lastName))
                customer.ChangeLastName(lastName, _uniquenessChecker);

            if (!customer.DateOfBirth.Equals(dateOfBirth))
                customer.ChangeDateOfBirth(dateOfBirth, _uniquenessChecker);

            if (!customer.PhoneNumber.Equals(phoneNumber))
                customer.ChangePhoneNumber(phoneNumber);

            if (!customer.Email.Equals(email))
                customer.ChangeEmail(email, _uniquenessChecker);

            if (!customer.BankAccountNumber.Equals(bankAccountNumber))
                customer.ChangeBankAccountNumber(bankAccountNumber);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}