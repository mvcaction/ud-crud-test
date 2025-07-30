using Domain.SeedWork.Primitives;
using Domain.Aggregates.Customer.ValueObjects;

namespace Domain.Aggregates.Customer.Events;

public sealed record CustomerUpdatedDomainEvent(
    Guid CustomerId,
    Email Email,
    FirstName FirstName,
    LastName LastName,
    DateTime UpdatedAt
) : DomainEventBase;