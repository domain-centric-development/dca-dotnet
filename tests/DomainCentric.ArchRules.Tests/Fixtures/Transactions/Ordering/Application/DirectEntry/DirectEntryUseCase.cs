using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.DirectEntry;

/// <summary>
/// DCA-USE-009: <c>ExecuteAsync</c> is public and saves without publishing. That <c>CompleteAsync</c> wraps it and
/// publishes afterwards does not help a caller who invokes <c>ExecuteAsync</c> directly - a public method stays an
/// entry point even when another method calls it.
/// </summary>
public sealed class DirectEntryUseCase
{
    private readonly IOrderRepository _orders;
    private readonly IDomainEventPublisher _events;

    public DirectEntryUseCase(IOrderRepository orders, IDomainEventPublisher events)
    {
        _orders = orders;
        _events = events;
    }

    public async Task<Guid> CompleteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = new Order(new OrderId(orderId));
        await ExecuteAsync(order, cancellationToken);
        await _events.PublishAndClearEventsAsync(order, cancellationToken);
        return order.Id.Value;
    }

    public Task ExecuteAsync(Order order, CancellationToken cancellationToken = default) => _orders.SaveAsync(order, cancellationToken);
}
