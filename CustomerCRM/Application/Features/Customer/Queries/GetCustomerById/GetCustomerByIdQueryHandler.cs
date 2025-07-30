using Application.Features.Customer.Abstractions;
using Application.Features.Customer.Models;
using MediatR;

namespace Application.Features.Customer.Queries.GetCustomerById;

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto?>
{
    private readonly ICustomerReadRepository _readRepository;

    public GetCustomerByIdQueryHandler(ICustomerReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<CustomerDto?> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        return await _readRepository.GetByIdAsync(request.Id, cancellationToken);
    }
}