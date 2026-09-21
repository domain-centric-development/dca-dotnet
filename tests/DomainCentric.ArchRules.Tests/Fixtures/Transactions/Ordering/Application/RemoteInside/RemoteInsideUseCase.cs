using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.RemoteInside;

/// <summary>A project's own declarative transaction attribute; no .NET preset configures one.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class UnitOfWorkAttribute : Attribute
{
}

/// <summary>A remote-capable output port: it leaves the process.</summary>
public interface ICarrierPort : IOutputPort
{
    Task<string> QuoteAsync(OrderId orderId, CancellationToken cancellationToken = default);
}

/// <summary>
/// DCA-USE-013: the whole class is declaratively transactional and the operation calls a
/// remote-capable port inside the transaction. Reported only once the attribute is configured.
/// </summary>
[UnitOfWork]
public sealed class RemoteInsideUseCase
{
    private readonly IOrderRepository _orders;
    private readonly ICarrierPort _carrier;

    public RemoteInsideUseCase(IOrderRepository orders, ICarrierPort carrier)
    {
        _orders = orders;
        _carrier = carrier;
    }

    public async Task<string> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = new Order(new OrderId(orderId));
        var quote = await _carrier.QuoteAsync(order.Id, cancellationToken);
        await _orders.SaveAsync(order, cancellationToken);
        return quote;
    }
}
