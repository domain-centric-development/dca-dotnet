using System;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Domain.Model;

public readonly record struct OrderLineId(Guid Value) : IId;

public sealed class OrderLine : IEntity<OrderLine, OrderLineId>
{
    public OrderLine(OrderLineId id)
    {
        Id = id;
    }

    public OrderLineId Id { get; }
}
