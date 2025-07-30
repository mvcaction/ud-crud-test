namespace Domain.SeedWork.Primitives;

public abstract record DomainEventBase : IDomainEvent
{
    public Guid Id { get; init; }
    public DateTime OccurredOn { get; init; }

    protected DomainEventBase()
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
    }

    protected DomainEventBase(Guid id, DateTime occurredOn)
    {
        Id = id;
        OccurredOn = occurredOn;
    }
}