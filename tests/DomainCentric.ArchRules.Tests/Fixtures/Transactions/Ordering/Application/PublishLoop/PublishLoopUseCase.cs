using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.PublishLoop;

/// <summary>
/// DCA-USE-009: saves, then dispatches the events by hand - <c>PublishAsync(event)</c> per event and an explicit
/// <c>ClearDomainEvents()</c>. Only <c>PublishAndClearEventsAsync</c> is a publication; this shape is reported although
/// every event does get published.
/// </summary>
public sealed class PublishLoopUseCase
{
    private readonly IOrderRepository _orders;
    private readonly IDomainEventPublisher _events;

    public PublishLoopUseCase(IOrderRepository orders, IDomainEventPublisher events)
    {
        _orders = orders;
        _events = events;
    }

    public async Task<Guid> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = new Order(new OrderId(orderId));
        await _orders.SaveAsync(order, cancellationToken);
        foreach (var domainEvent in order.DomainEvents)
        {
            await _events.PublishAsync(domainEvent, cancellationToken);
        }

        order.ClearDomainEvents();
        return order.Id.Value;
    }
}
