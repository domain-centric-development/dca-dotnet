using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.RecursiveSave;

/// <summary>Valid: the saving helper calls itself; the entry method publishes. The walk must terminate.</summary>
public sealed class RecursiveSaveUseCase
{
    private readonly IOrderRepository _orders;
    private readonly IDomainEventPublisher _events;

    public RecursiveSaveUseCase(IOrderRepository orders, IDomainEventPublisher events)
    {
        _orders = orders;
        _events = events;
    }

    public async Task<Guid> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = new Order(new OrderId(orderId));
        await PersistWithRetryAsync(order, 3, cancellationToken);
        await _events.PublishAndClearEventsAsync(order, cancellationToken);
        return order.Id.Value;
    }

    private async Task PersistWithRetryAsync(Order order, int attemptsLeft, CancellationToken cancellationToken)
    {
        if (attemptsLeft > 1)
        {
            await PersistWithRetryAsync(order, attemptsLeft - 1, cancellationToken);
        }

        await _orders.SaveAsync(order, cancellationToken);
    }
}
