using Application.Features.Customer.Models;
using MediatR;

namespace Application.Features.Customer.Queries.GetCustomersList;

public record GetCustomersListQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    bool IncludeDeleted = false) : IRequest<PagedResult<CustomerListDto>>;