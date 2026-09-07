using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.MutualRecursion;

/// <summary>
/// DCA-USE-009: <c>PingAsync</c> and <c>PongAsync</c> call each other, <c>PingAsync</c> saves, nothing publishes.
/// No method of the cycle is an entry point; the rule must still terminate and report.
/// </summary>
public sealed class MutualRecursionUseCase
{
    private readonly IOrderRepository _orders;

    public MutualRecursionUseCase(IOrderRepository orders)
    {
        _orders = orders;
    }

    private async Task PingAsync(Order order, int depth, CancellationToken cancellationToken)
    {
        await _orders.SaveAsync(order, cancellationToken);
        if (depth > 0)
        {
            await PongAsync(order, depth - 1, cancellationToken);
        }
    }

    private async Task PongAsync(Order order, int depth, CancellationToken cancellationToken)
    {
        if (depth > 0)
        {
            await PingAsync(order, depth - 1, cancellationToken);
        }
    }
}
