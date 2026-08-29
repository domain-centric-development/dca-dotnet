using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Application.PlaceOrder;

public sealed record PlaceOrderCommand(string OrderId);

public sealed record PlaceOrderResult(string OrderId, bool Placed);

public interface IPlaceOrderInputPort : IUseCase<PlaceOrderCommand, PlaceOrderResult>
{
}

public sealed class PlaceOrderUseCase : IPlaceOrderInputPort
{
    private readonly IOrderRepository _orders;
    private readonly IOrderAuditStore _audit;

    public PlaceOrderUseCase(IOrderRepository orders, IOrderAuditStore audit)
    {
        _orders = orders;
        _audit = audit;
    }

    public async Task<PlaceOrderResult> ExecuteAsync(PlaceOrderCommand input, CancellationToken cancellationToken = default)
    {
        var id = new OrderId(input.OrderId);
        var order = await _orders.FindByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new System.InvalidOperationException("Order not found");
        order.Place();
        await _orders.SaveAsync(order, cancellationToken).ConfigureAwait(false);
        await _audit.RecordAsync(id, "placed", cancellationToken).ConfigureAwait(false);
        return new PlaceOrderResult(id.Value, order.IsPlaced);
    }
}
