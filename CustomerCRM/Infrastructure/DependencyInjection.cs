using Application.Features.Customer.Abstractions;
using Domain.Aggregates.Customer.Services;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Resilience;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddServices();
        services.AddResilience(configuration);
        
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
                
                // PostgreSQL retry configuration for transient errors
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
        services.AddScoped<IExternalCustomerValidationService, ExternalCustomerValidationService>();
        
        return services;
    }

    private static IServiceCollection AddResilience(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure resilience options - Fix the configuration binding
        services.Configure<ResilienceOptions>(
            configuration.GetSection(ResilienceOptions.SectionName));

        // Register resilience service
        services.AddSingleton<IResilienceService, ResilienceService>();

        // Configure HttpClient with resilience policies
        services.AddHttpClient("default", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(2); // Overall timeout
        })
        .AddPolicyHandler((serviceProvider, request) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ResilienceOptions>>().Value;
            var logger = serviceProvider.GetRequiredService<ILogger<ResilienceService>>();
            return HttpResiliencePolicies.GetCombinedPolicy(options, logger);
        });

        // Add specific HttpClient configurations for different services
        services.AddHttpClient("external-api", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            // Configure base address, headers, etc. for external APIs
            // client.BaseAddress = new Uri("https://external-api.example.com/");
        })
        .AddPolicyHandler((serviceProvider, request) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ResilienceOptions>>().Value;
            var logger = serviceProvider.GetRequiredService<ILogger<ResilienceService>>();
            return HttpResiliencePolicies.GetCombinedPolicy(options, logger);
        });

        return services;
    }
}