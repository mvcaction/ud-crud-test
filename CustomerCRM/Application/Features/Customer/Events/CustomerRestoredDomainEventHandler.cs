using Domain.Aggregates.Customer.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Customer.Events;

public class CustomerRestoredDomainEventHandler : INotificationHandler<CustomerRestoredDomainEvent>
{
    private readonly ILogger<CustomerRestoredDomainEventHandler> _logger;

    public CustomerRestoredDomainEventHandler(ILogger<CustomerRestoredDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(CustomerRestoredDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Customer restored: {CustomerId} - {Email} at {RestoredAt}", 
            notification.CustomerId, 
            notification.Email.Value, 
            notification.RestoredAt);

        // Add any additional business logic here
        
        await Task.CompletedTask;
    }
}