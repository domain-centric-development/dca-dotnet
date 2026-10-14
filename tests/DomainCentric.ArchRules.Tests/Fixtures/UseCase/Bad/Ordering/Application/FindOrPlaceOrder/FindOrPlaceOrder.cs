using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.FindOrPlaceOrder;

public interface IFindOrPlaceOrderInputPort : IUseCase<FindOrPlaceOrderQuery, FindOrPlaceOrderResult>
{
}

public sealed record FindOrPlaceOrderQuery(Guid OrderId);

public sealed record FindOrPlaceOrderResult(Guid OrderId);

/// <summary>DCA-USE-019: a query that creates what it does not find - find-or-create is a command.</summary>
public sealed class FindOrPlaceOrderUseCase : IFindOrPlaceOrderInputPort
{
    private readonly IOrderRepository _orders;
    private readonly IDomainEventPublisher _events;

    public FindOrPlaceOrderUseCase(IOrderRepository orders, IDomainEventPublisher events)
    {
        _orders = orders;
        _events = events;
    }

    public async Task<FindOrPlaceOrderResult> ExecuteAsync(FindOrPlaceOrderQuery query, CancellationToken cancellationToken = default)
    {
        var id = new OrderId(query.OrderId);
        var order = await _orders.FindByIdAsync(id, cancellationToken);
        if (order is null)
        {
            order = Order.Place(id);
            await _orders.SaveAsync(order, cancellationToken);
            await _events.PublishAndClearEventsAsync(order, cancellationToken);
        }

        return new FindOrPlaceOrderResult(order.Id.Value);
    }
}
