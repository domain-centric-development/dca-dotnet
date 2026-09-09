using System;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Domain.Model;

public readonly record struct OrderId(Guid Value) : IId;

public sealed class Order : AggregateRootBase<Order, OrderId>
{
    public Order(OrderId id)
    {
        Id = id;
    }

    public override OrderId Id { get; }    public void RecordChange() { RegisterEvent(new Changed(Guid.NewGuid(), DateTimeOffset.UtcNow)); }
    private sealed record Changed(Guid EventId, DateTimeOffset OccurredOn) : IDomainEvent;

}
