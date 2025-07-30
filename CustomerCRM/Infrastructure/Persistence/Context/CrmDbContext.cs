using Domain.Aggregates.Customer;
using Infrastructure.Persistence.EntityConfiguration;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Context;

public class CrmDbContext : DbContext
{
    public DbSet<Customer> Customers { get; set; } = null!;

    public CrmDbContext(DbContextOptions<CrmDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CustomerEntityConfiguration());
        // In your DbContext OnModelCreating:
        modelBuilder.ApplyConfiguration(new CustomerEntityConfiguration());

        // Configure global query filter for soft delete
        modelBuilder.Entity<Customer>()
            .HasQueryFilter(c => !c.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Domain events will be handled by UnitOfWork
        // This method only saves the actual data changes
        return await base.SaveChangesAsync(cancellationToken);
    }
}