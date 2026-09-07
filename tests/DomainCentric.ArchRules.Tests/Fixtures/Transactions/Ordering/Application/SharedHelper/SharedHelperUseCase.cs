using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.SharedHelper;

/// <summary>
/// DCA-USE-009: <c>ExecuteAsync</c> saves and never publishes. The private <c>Validate</c> helper it shares with
/// <c>SeparateAsync</c> - which does publish - must not connect the two: a callee in common is not an execution path.
/// </summary>
public sealed class SharedHelperUseCase
{
    private readonly IOrderRepository _orders;
    private readonly IDomainEventPublisher _events;

    public SharedHelperUseCase(IOrderRepository orders, IDomainEventPublisher events)
    {
        _orders = orders;
        _events = events;
    }

    public async Task<Guid> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        Validate(orderId);
        var order = new Order(new OrderId(orderId));
        await _orders.SaveAsync(order, cancellationToken);
        return order.Id.Value;
    }

    public async Task SeparateAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        Validate(orderId);
        var order = await _orders.FindByIdAsync(new OrderId(orderId), cancellationToken) ?? throw new InvalidOperationException();
        await _events.PublishAndClearEventsAsync(order, cancellationToken);
    }

    private static void Validate(Guid orderId)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("orderId");
        }
    }
}
