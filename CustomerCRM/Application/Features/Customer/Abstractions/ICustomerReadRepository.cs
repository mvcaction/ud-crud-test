using Application.Features.Customer.Models;

namespace Application.Features.Customer.Abstractions;

public interface ICustomerReadRepository
{
    Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<PagedResult<CustomerListDto>> GetPagedListAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        bool includeDeleted,
        CancellationToken cancellationToken = default);
}