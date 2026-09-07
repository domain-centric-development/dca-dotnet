using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.SharedSaveHelper;

/// <summary>
/// DCA-USE-009: two entry methods share the saving helper <c>PersistAsync</c>. <c>ExecuteAsync</c> publishes after it,
/// <c>ExecuteQuietlyAsync</c> does not - the covered caller must not cover the other.
/// </summary>
public sealed class SharedSaveHelperUseCase
{
    private readonly IOrderRepository _orders;
    private readonly IDomainEventPublisher _events;

    public SharedSaveHelperUseCase(IOrderRepository orders, IDomainEventPublisher events)
    {
        _orders = orders;
        _events = events;
    }

    public async Task<Guid> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = new Order(new OrderId(orderId));
        await PersistAsync(order, cancellationToken);
        await _events.PublishAndClearEventsAsync(order, cancellationToken);
        return order.Id.Value;
    }

    public async Task<Guid> ExecuteQuietlyAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = new Order(new OrderId(orderId));
        await PersistAsync(order, cancellationToken);
        return order.Id.Value;
    }

    private Task PersistAsync(Order order, CancellationToken cancellationToken) => _orders.SaveAsync(order, cancellationToken);
}
