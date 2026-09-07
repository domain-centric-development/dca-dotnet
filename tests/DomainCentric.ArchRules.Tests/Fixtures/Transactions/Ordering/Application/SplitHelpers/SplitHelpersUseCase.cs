using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.SplitHelpers;

/// <summary>Valid: <c>ExecuteAsync</c> saves through one helper and publishes through another.</summary>
public sealed class SplitHelpersUseCase
{
    private readonly IOrderRepository _orders;
    private readonly IDomainEventPublisher _events;

    public SplitHelpersUseCase(IOrderRepository orders, IDomainEventPublisher events)
    {
        _orders = orders;
        _events = events;
    }

    public async Task<Guid> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = new Order(new OrderId(orderId));
        await PersistAsync(order, cancellationToken);
        await AnnounceAsync(order, cancellationToken);
        return order.Id.Value;
    }

    private Task PersistAsync(Order order, CancellationToken cancellationToken) => _orders.SaveAsync(order, cancellationToken);

    private Task AnnounceAsync(Order order, CancellationToken cancellationToken) => _events.PublishAndClearEventsAsync(order, cancellationToken);
}
