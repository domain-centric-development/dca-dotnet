using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Domain.Model;

public sealed class Order : AggregateRootBase<Order, OrderId>
{
    private Order(OrderId id)
    {
        Id = id;
    }

    public override OrderId Id { get; }

    public static Order Place(OrderId id)
    {
        var order = new Order(id);
        order.RegisterEvent(new OrderPlaced(id));
        return order;
    }
}
