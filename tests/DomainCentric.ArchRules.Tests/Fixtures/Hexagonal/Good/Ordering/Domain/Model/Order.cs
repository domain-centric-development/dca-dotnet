using System;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.SharedKernel.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Ordering.Domain.Model;

public sealed class Order : AggregateRootBase<Order, OrderId>
{
    private Order(OrderId id, Money total)
    {
        Id = id;
        Total = total;
    }

    public override OrderId Id { get; }

    public Money Total { get; }

    public static Order Place(Money total)
    {
        var order = new Order(new OrderId(Guid.NewGuid()), total);
        order.RegisterEvent(new OrderPlaced(Guid.NewGuid(), DateTimeOffset.UtcNow, order.Id));
        return order;
    }
}
