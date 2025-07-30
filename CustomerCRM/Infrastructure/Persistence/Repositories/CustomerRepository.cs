using Application.Features.Customer.Abstractions;
using Domain.Aggregates.Customer;
using Domain.Aggregates.Customer.ValueObjects;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Infrastructure.Services;

namespace Infrastructure.Persistence.Repositories;

internal class CustomerRepository : ICustomerRepository
{
    private readonly CrmDbContext _context;
    private readonly ICustomerHydrationService _hydrationService;
    private readonly ILogger<CustomerRepository> _logger;

    public CustomerRepository(
        CrmDbContext context, 
        ICustomerHydrationService hydrationService,
        ILogger<CustomerRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _hydrationService = hydrationService ?? throw new ArgumentNullException(nameof(hydrationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default, bool includeDeleted = false)
    {
        try
        {
            var query = _context.Customers.AsQueryable();

            if (includeDeleted)
            {
                query = query.IgnoreQueryFilters();
            }

            var customer = await query.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            
            if (customer is not null)
            {
                _hydrationService.Hydrate(customer);
            }

            return customer;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operation was cancelled while retrieving customer {CustomerId}", id);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error retrieving customer {CustomerId}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving customer {CustomerId}", id);
            throw;
        }
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            // Fix: Create Email value object for comparison
            // EF Core will use the value conversion to compare against the string value in the database
            var emailValueObject = Email.Create(email);
            
            var customer = await _context.Customers
                .Where(c => c.Email == emailValueObject)
                .FirstOrDefaultAsync(cancellationToken);
                
            if (customer is not null)
            {
                _hydrationService.Hydrate(customer);
            }

            return customer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer by email {Email}", email);
            throw;
        }
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customer);
        try
        {
            await _context.Customers.AddAsync(customer, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding customer");
            throw;
        }
    }

    public void Update(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        try
        {
            _context.Customers.Update(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", customer.Id);
            throw;
        }
    }
}