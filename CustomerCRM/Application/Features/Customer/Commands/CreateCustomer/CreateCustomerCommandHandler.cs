using Application.Common;
using Application.Features.Customer.Abstractions;
using Domain.Aggregates.Customer.Services;
using Domain.Aggregates.Customer.ValueObjects;
using Domain.SeedWork.Exceptions;
using MediatR;

namespace Application.Features.Customer.Commands.CreateCustomer;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Result<Guid>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerUniquenessCheckerService _uniquenessChecker;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        ICustomerUniquenessCheckerService uniquenessChecker,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _uniquenessChecker = uniquenessChecker;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var firstName = FirstName.Create(request.FirstName);
            var lastName = LastName.Create(request.LastName);
            var dateOfBirth = DateOfBirth.Create(request.DateOfBirth);
            var phoneNumber = PhoneNumber.Create(request.PhoneNumber);
            var email = Email.Create(request.Email);
            var bankAccountNumber = BankAccountNumber.Create(request.BankAccountNumber);

            var customer = Domain.Aggregates.Customer.Customer.Create(
                firstName,
                lastName,
                dateOfBirth,
                phoneNumber,
                email,
                bankAccountNumber,
                _uniquenessChecker,
                _dateTimeProvider);

            await _customerRepository.AddAsync(customer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(customer.Id);
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result.Failure<Guid>(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<Guid>(ex.Message);
        }
    }
}