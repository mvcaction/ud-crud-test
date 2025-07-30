using API.IntegrationTests.Common;
using Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace API.IntegrationTests.Common;

public class TestContainerWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup>, IAsyncLifetime
    where TStartup : class
{
    private readonly PostgreSqlTestContainer _testContainer;

    public TestContainerWebApplicationFactory()
    {
        _testContainer = new PostgreSqlTestContainer();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CrmDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add DbContext with TestContainer connection string
            services.AddDbContext<CrmDbContext>(options =>
                options.UseNpgsql(_testContainer.ConnectionString));

            // Override logging to reduce noise during tests
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        });

        builder.UseEnvironment("Testing");

        // Use the API project's content root instead of the test project's
        var apiProjectPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "API"));
        if (Directory.Exists(apiProjectPath))
        {
            builder.UseContentRoot(apiProjectPath);
        }

        // Disable host configuration validation that might require deps.json
        builder.UseSetting("hostBuilder:reloadConfigOnChange", "false");
    }

    public async Task InitializeAsync()
    {
        await _testContainer.InitializeAsync();
    }

    public new async Task DisposeAsync()
    {
        await _testContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}