using DotNet.Testcontainers.Builders;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;

namespace Infrastructure.IntegrationTests.Common;

public class PostgreSqlTestContainer : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public PostgreSqlTestContainer()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("CustomerCRM_Test")
            .WithUsername("postgres")
            .WithPassword("postgres123")
            .WithPortBinding(0, 5432) // Use random available port
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .WithCleanUp(true)
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Ensure database is ready by creating and testing a context
        var services = new ServiceCollection();
        services.AddDbContext<CrmDbContext>(options =>
            options.UseNpgsql(ConnectionString));
        services.AddLogging();

        using var serviceProvider = services.BuildServiceProvider();
        using var context = serviceProvider.GetRequiredService<CrmDbContext>();

        // Apply migrations
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}