using System;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Infrastructure.Config;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Adapter.Outgoing;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Application.PlaceOrder;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using Microsoft.EntityFrameworkCore;

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

    public bool Cancelled { get; private set; }

    public string? Note { get; set; }

    private int _reminders;

    /// <summary>DCA-HEX-021: a state change through the aggregate's own method.</summary>
    public void Cancel() => Cancelled = true;

    /// <summary>DCA-HEX-021: returns a value, but writes a field on the way.</summary>
    public int Remind() => ++_reminders;

    /// <summary>DCA-HEX-021: a query - returns a value and writes nothing.</summary>
    public bool IsLargerThan(Money other) => Total.Amount > other.Amount;

    public static Order Place(Money total)
    {
        _ = InMemoryConfig.OrderRepository(); // DCA-LAY-002: domain -> infrastructure
        var order = new Order(new OrderId(Guid.NewGuid()), total);
        order.RegisterEvent(new OrderPlaced(Guid.NewGuid(), DateTimeOffset.UtcNow, order.Id));
        return order;
    }
}
