using Domain.Aggregates.Customer.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Customer.Events;

public class CustomerDeletedDomainEventHandler : INotificationHandler<CustomerDeletedDomainEvent>
{
    private readonly ILogger<CustomerDeletedDomainEventHandler> _logger;

    public CustomerDeletedDomainEventHandler(ILogger<CustomerDeletedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(CustomerDeletedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Customer deleted: {CustomerId} - {Email} at {DeletedAt}", 
            notification.CustomerId, 
            notification.Email.Value, 
            notification.DeletedAt);

        // Add any additional business logic here
        // For example: cleanup related data, send notifications, etc.
        
        await Task.CompletedTask;
    }
}