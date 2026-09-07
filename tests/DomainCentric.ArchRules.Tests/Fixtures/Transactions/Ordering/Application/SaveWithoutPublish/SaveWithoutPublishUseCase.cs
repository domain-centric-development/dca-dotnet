using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.SaveWithoutPublish;

/// <summary>
/// DCA-USE-009: <c>ExecuteAsync</c> saves and never publishes; the publication in the unconnected
/// <c>RepublishAsync</c> method must not count for it.
/// </summary>
public sealed class SaveWithoutPublishUseCase
{
    private readonly IOrderRepository _orders;
    private readonly IDomainEventPublisher _events;

    public SaveWithoutPublishUseCase(IOrderRepository orders, IDomainEventPublisher events)
    {
        _orders = orders;
        _events = events;
    }

    public async Task<Guid> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = new Order(new OrderId(orderId));
        await _orders.SaveAsync(order, cancellationToken);
        return order.Id.Value;
    }

    public async Task RepublishAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orders.FindByIdAsync(new OrderId(orderId), cancellationToken) ?? throw new InvalidOperationException();
        await _events.PublishAndClearEventsAsync(order, cancellationToken);
    }
}
