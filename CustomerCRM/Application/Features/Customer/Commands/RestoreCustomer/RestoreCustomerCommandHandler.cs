using Application.Common;
using Application.Features.Customer.Abstractions;
using Domain.SeedWork.Exceptions;
using MediatR;

namespace Application.Features.Customer.Commands.RestoreCustomer;

public class RestoreCustomerCommandHandler : IRequestHandler<RestoreCustomerCommand, Result>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RestoreCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RestoreCustomerCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken, includeDeleted: true);
            if (customer == null)
                return Result.Failure("Customer not found");

            customer.Restore();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}