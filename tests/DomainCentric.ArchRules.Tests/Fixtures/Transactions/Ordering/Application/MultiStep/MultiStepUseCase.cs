using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.MultiStep;

/// <summary>Valid: save and publish each sit two delegation steps below the entry method.</summary>
public sealed class MultiStepUseCase
{
    private readonly IOrderRepository _orders;
    private readonly IDomainEventPublisher _events;

    public MultiStepUseCase(IOrderRepository orders, IDomainEventPublisher events)
    {
        _orders = orders;
        _events = events;
    }

    public async Task<Guid> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = new Order(new OrderId(orderId));
        await StoreAsync(order, cancellationToken);
        await BroadcastAsync(order, cancellationToken);
        return order.Id.Value;
    }

    private Task StoreAsync(Order order, CancellationToken cancellationToken) => PersistAsync(order, cancellationToken);

    private async Task PersistAsync(Order order, CancellationToken cancellationToken) => await _orders.SaveAsync(order, cancellationToken);

    private Task BroadcastAsync(Order order, CancellationToken cancellationToken) => AnnounceAsync(order, cancellationToken);

    private async Task AnnounceAsync(Order order, CancellationToken cancellationToken) => await _events.PublishAndClearEventsAsync(order, cancellationToken);
}
