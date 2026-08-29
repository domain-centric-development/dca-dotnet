using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using DomainCentric.ArchRules.Tests.Fixtures.Naming.Good.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Naming.Good.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Naming.Good.Ordering.Application.PlaceOrder;

public interface IPlaceOrderInputPort : IUseCase<PlaceOrderCommand, PlaceOrderResult>
{
}

public sealed record PlaceOrderCommand(Guid OrderId);

public sealed record PlaceOrderResult(Guid OrderId);

public sealed class PlaceOrderUseCase : IPlaceOrderInputPort
{
    private readonly IOrderRepository _orders;
    private readonly IDomainEventPublisher _events;

    public PlaceOrderUseCase(IOrderRepository orders, IDomainEventPublisher events)
    {
        _orders = orders;
        _events = events;
    }

    public async Task<PlaceOrderResult> ExecuteAsync(PlaceOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = Order.Place(new OrderId(command.OrderId));
        await _orders.SaveAsync(order, cancellationToken);
        await _events.PublishAndClearEventsAsync(order, cancellationToken);
        return new PlaceOrderResult(order.Id.Value);
    }
}
