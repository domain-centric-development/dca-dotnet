using System;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using Microsoft.EntityFrameworkCore;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Infrastructure.Config;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Adapter.Outgoing;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Application.PlaceOrder;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.SharedKernel.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Domain.Model;

[Keyless] // DCA-ONI-002 / DCA-ONI-003: framework attribute on a domain model
public sealed class Order : AggregateRootBase<Order, OrderId>
{
    private readonly InMemoryOrderRepository _repository = new(); // DCA-HEX-001: domain -> adapter
    private readonly Func<PlaceOrderCommand, Money> _fromCommand = c => c.Total; // DCA-ONI-001: domain -> application

    private Order(OrderId id, Money total)
    {
        Id = id;
        Total = total;
    }

    public override OrderId Id { get; }

    public Money Total { get; }

    public static Order Place(Money total)
    {
        _ = InMemoryConfig.OrderRepository(); // DCA-LAY-002: domain -> infrastructure
        var order = new Order(new OrderId(Guid.NewGuid()), total);
        order.RegisterEvent(new OrderPlaced(Guid.NewGuid(), DateTimeOffset.UtcNow, order.Id));
        return order;
    }
}
