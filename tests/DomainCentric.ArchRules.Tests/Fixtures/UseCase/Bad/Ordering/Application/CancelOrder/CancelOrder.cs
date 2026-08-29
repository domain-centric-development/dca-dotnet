using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Adapter.Incoming.Web;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.CancelOrder;

// DCA-USE-004: mutable, non-sealed command
public class CancelOrderCommand
{
    public Guid OrderId { get; set; }
}

// DCA-USE-007: mutable, non-sealed result
public class CancelOrderResult
{
    public Guid OrderId { get; set; }
}

// DCA-USE-008: a Response model in the application layer
public sealed record CancelOrderResponse(Guid OrderId);

// DCA-USE-009: saves without publishing; DCA-USE-011: depends on a DTO
public sealed class CancelOrderUseCase
{
    private readonly IOrderRepository _orders;

    public CancelOrderUseCase(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<OrderDto> ExecuteAsync(CancelOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = Order.Place(new OrderId(command.OrderId));
        await _orders.SaveAsync(order, cancellationToken);
        return new OrderDto(order.Id.Value);
    }
}
