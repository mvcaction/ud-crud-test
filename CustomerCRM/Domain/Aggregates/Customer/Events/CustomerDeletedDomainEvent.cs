using Domain.SeedWork.Primitives;
using Domain.Aggregates.Customer.ValueObjects;

namespace Domain.Aggregates.Customer.Events;

public sealed record CustomerDeletedDomainEvent(
    Guid CustomerId,
    Email Email,
    DateTime DeletedAt
) : DomainEventBase;