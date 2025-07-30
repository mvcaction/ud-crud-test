using Application.Features.Customer.Abstractions;
using Application.Features.Customer.Models;
using MediatR;

namespace Application.Features.Customer.Queries.GetCustomerByEmail;

public class GetCustomerByEmailQueryHandler : IRequestHandler<GetCustomerByEmailQuery, CustomerDto?>
{
    private readonly ICustomerReadRepository _readRepository;

    public GetCustomerByEmailQueryHandler(ICustomerReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<CustomerDto?> Handle(GetCustomerByEmailQuery request, CancellationToken cancellationToken)
    {
        return await _readRepository.GetByEmailAsync(request.Email, cancellationToken);
    }
}