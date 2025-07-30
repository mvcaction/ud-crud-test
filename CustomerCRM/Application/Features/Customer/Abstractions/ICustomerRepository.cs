namespace Application.Features.Customer.Abstractions;

public interface ICustomerRepository
{
    Task<Domain.Aggregates.Customer.Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default, bool includeDeleted = false);
    Task<Domain.Aggregates.Customer.Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(Domain.Aggregates.Customer.Customer customer, CancellationToken cancellationToken = default);
    void Update(Domain.Aggregates.Customer.Customer customer);
}