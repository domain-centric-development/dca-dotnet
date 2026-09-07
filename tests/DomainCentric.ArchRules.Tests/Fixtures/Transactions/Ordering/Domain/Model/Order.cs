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

    public override OrderId Id { get; }
}
