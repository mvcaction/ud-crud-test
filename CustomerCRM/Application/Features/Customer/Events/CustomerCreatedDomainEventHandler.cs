using Domain.Aggregates.Customer.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Customer.Events;

public class CustomerCreatedDomainEventHandler : INotificationHandler<CustomerCreatedDomainEvent>
{
    private readonly ILogger<CustomerCreatedDomainEventHandler> _logger;

    public CustomerCreatedDomainEventHandler(ILogger<CustomerCreatedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(CustomerCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Customer created: {CustomerId} - {Email} at {CreatedAt}", 
            notification.CustomerId, 
            notification.Email.Value, 
            notification.CreatedAt);

        // Add any additional business logic here
        // For example: sending welcome email, creating audit log, etc.
        
        await Task.CompletedTask;
    }
}