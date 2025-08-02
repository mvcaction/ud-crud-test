using Application.Features.Customer.Abstractions;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

internal class UnitOfWork : IUnitOfWork
{
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(ILogger<UnitOfWork> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Since we're using Dapper, the actual save operations happen in the repositories
            // This method can be used for cross-cutting concerns like transaction management
            // For now, it's a no-op since Dapper executes commands immediately
            await Task.CompletedTask;
            
            _logger.LogDebug("Unit of work save changes completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during unit of work save changes");
            throw;
        }
    }
}