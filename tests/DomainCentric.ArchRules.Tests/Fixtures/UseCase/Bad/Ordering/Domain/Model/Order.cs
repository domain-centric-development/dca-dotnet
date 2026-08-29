using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Adapter.Incoming.Web;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Domain.Model;

// DCA-USE-010: domain depends on a DTO
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

    public OrderDto ToDto() => new(Id.Value);
}
