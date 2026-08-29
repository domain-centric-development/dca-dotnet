using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Ordering.Application.PlaceOrder;

public sealed class PlaceOrderUseCase : IPlaceOrderInputPort
{
    private readonly IOrderRepository _orders;

    public PlaceOrderUseCase(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<PlaceOrderResult> ExecuteAsync(PlaceOrderCommand input, CancellationToken cancellationToken = default)
    {
        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        var order = Order.Place(input.Total);
        await _orders.SaveAsync(order, cancellationToken).ConfigureAwait(false);
        scope.Complete();
        return new PlaceOrderResult(order.Id);
    }
}
