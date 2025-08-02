using Application.Common;
using Application.Features.Customer.Abstractions;
using Domain.Aggregates.Customer.Services;
using Domain.Aggregates.Customer.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Customer.Commands.CreateCustomer;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Result<Guid>>
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICustomerUniquenessCheckerService _uniquenessChecker;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CreateCustomerCommandHandler> _logger;

    public CreateCustomerCommandHandler(
        ICustomerRepository repository,
        IUnitOfWork unitOfWork,
        ICustomerUniquenessCheckerService uniquenessChecker,
        IDateTimeProvider dateTimeProvider,
        ILogger<CreateCustomerCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _uniquenessChecker = uniquenessChecker;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var customer = Domain.Aggregates.Customer.Customer.Create(
                FirstName.Create(request.FirstName),
                LastName.Create(request.LastName),
                DateOfBirth.Create(request.DateOfBirth),
                PhoneNumber.Create(request.PhoneNumber),
                Email.Create(request.Email),
                BankAccountNumber.Create(request.BankAccountNumber),
                _uniquenessChecker,
                _dateTimeProvider
            );

            await _repository.AddAsync(customer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Customer created successfully with ID {CustomerId}", customer.Id);
            return Result<Guid>.Success(customer.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return Result<Guid>.Failure<Guid>(ex.Message);
        }
    }
}