using System;
using System.Collections.Generic;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Domain.Model;

public sealed class Order : AggregateRootBase<Order, OrderId>
{
    private readonly List<LineItem> _lineItems = new();

    public Order(OrderId id, CustomerId customerId)
    {
        Id = id;
        CustomerId = customerId;
    }

    public override OrderId Id { get; }

    public CustomerId CustomerId { get; }

    public IReadOnlyList<LineItem> LineItems => _lineItems.AsReadOnly();

    public bool IsPlaced { get; private set; }

    public void AddLineItem(LineItemId lineItemId, string sku, Quantity quantity) =>
        _lineItems.Add(new LineItem(lineItemId, sku, quantity));

    public void Place()
    {
        if (_lineItems.Count == 0)
        {
            throw new InvalidOperationException("Cannot place an empty order");
        }

        IsPlaced = true;
        RegisterEvent(new OrderPlaced(Guid.NewGuid(), DateTimeOffset.UtcNow, Id));
    }
}
