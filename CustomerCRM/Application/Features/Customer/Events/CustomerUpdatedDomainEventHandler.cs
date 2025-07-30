using Domain.Aggregates.Customer.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Customer.Events;

public class CustomerUpdatedDomainEventHandler : INotificationHandler<CustomerUpdatedDomainEvent>
{
    private readonly ILogger<CustomerUpdatedDomainEventHandler> _logger;

    public CustomerUpdatedDomainEventHandler(ILogger<CustomerUpdatedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(CustomerUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Customer updated: {CustomerId} - {Email} at {UpdatedAt}", 
            notification.CustomerId, 
            notification.Email.Value, 
            notification.UpdatedAt);

        // Add any additional business logic here
        // For example: sending notification, updating cache, etc.
        
        await Task.CompletedTask;
    }
}