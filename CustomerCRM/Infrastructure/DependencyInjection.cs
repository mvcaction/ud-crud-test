using Application.Features.Customer.Abstractions;
using Domain.Aggregates.Customer.Services;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Keep EF Core only for migrations - remove DbContext from regular operations
        services.AddDbContext<CrmDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("Infrastructure")));

        // Register Dapper-based repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerReadRepository, CustomerReadRepository>();
        
        // Register services
        services.AddScoped<ICustomerUniquenessCheckerService, CustomerUniquenessCheckerService>();
        services.AddScoped<ICustomerHydrationService, CustomerHydrationService>();
        services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();
        
        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}