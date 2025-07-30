using Domain.SeedWork.Primitives;
using Domain.Aggregates.Customer.ValueObjects;

namespace Domain.Aggregates.Customer.Events;

public sealed record CustomerRestoredDomainEvent(
    Guid CustomerId,
    Email Email,
    DateTime RestoredAt
) : DomainEventBase;