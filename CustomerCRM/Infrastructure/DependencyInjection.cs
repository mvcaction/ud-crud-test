using Application.Features.Customer.Abstractions;
using Domain.Aggregates.Customer.Services;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddServices();
        
        return services;
    }

    private static IServiceCollection AddPersistence(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found");

        services.AddDbContext<CrmDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(CrmDbContext).Assembly.FullName);
                
                // PostgreSQL retry configuration ?? error codes
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: new[] { 
                        "57P01", // admin_shutdown
                        "57P02", // crash_shutdown  
                        "57P03", // cannot_connect_now
                        "58000", // system_error
                        "58030"  // io_error
                    });
                    
                npgsqlOptions.CommandTimeout(30);
            });
            
            options.EnableServiceProviderCaching();
            options.EnableSensitiveDataLogging(false);
        });

        // Repository registrations
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerReadRepository, CustomerReadRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<ICustomerUniquenessCheckerService, CustomerUniquenessCheckerService>();
        services.AddScoped<ICustomerHydrationService, CustomerHydrationService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        
        return services;
    }
}