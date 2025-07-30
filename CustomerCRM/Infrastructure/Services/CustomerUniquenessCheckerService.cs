using Domain.Aggregates.Customer.Services;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class CustomerUniquenessCheckerService : ICustomerUniquenessCheckerService
{
    private readonly CrmDbContext _context;

    public CustomerUniquenessCheckerService(CrmDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<bool> IsEmailTaken(string email, Guid? excludeCustomerId = null)
    {
        // Use client evaluation by loading customers first then filtering in memory
        var customers = await _context.Customers
            .IgnoreQueryFilters()
            .AsNoTracking() // For better performance
            .ToListAsync();

        // Now filter in memory where Email.ToString() works
        return customers.Any(c =>
            c.Email.ToString() == email &&
            (!excludeCustomerId.HasValue || c.Id != excludeCustomerId.Value));
    }

    public async Task<bool> IsPersonalInfoTaken(
        string firstName,
        string lastName,
        DateTime dateOfBirth,
        Guid? excludeCustomerId = null)
    {
        // Use client evaluation for this query too for consistency
        var customers = await _context.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync();

        return customers.Any(c =>
            c.FirstName.ToString() == firstName &&
            c.LastName.ToString() == lastName &&
            c.DateOfBirth.Value == dateOfBirth &&
            (!excludeCustomerId.HasValue || c.Id != excludeCustomerId.Value));
    }
}