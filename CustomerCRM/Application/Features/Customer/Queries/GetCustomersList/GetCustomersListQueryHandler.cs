using Application.Features.Customer.Abstractions;
using Application.Features.Customer.Models;
using MediatR;

namespace Application.Features.Customer.Queries.GetCustomersList;

public class GetCustomersListQueryHandler : IRequestHandler<GetCustomersListQuery, PagedResult<CustomerListDto>>
{
    private readonly ICustomerReadRepository _readRepository;

    public GetCustomersListQueryHandler(ICustomerReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<PagedResult<CustomerListDto>> Handle(GetCustomersListQuery request, CancellationToken cancellationToken)
    {
        return await _readRepository.GetPagedListAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.IncludeDeleted,
            cancellationToken);
    }
}