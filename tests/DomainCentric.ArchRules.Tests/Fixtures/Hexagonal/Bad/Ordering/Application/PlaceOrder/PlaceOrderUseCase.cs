using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Infrastructure.Config;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Adapter.Outgoing;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Application.PlaceOrder;

public sealed class PlaceOrderUseCase : IPlaceOrderInputPort
{
    private readonly IOrderRepository _orders = new InMemoryOrderRepository(); // DCA-HEX-002: application -> adapter

    public async Task<PlaceOrderResult> ExecuteAsync(PlaceOrderCommand input, CancellationToken cancellationToken = default)
    {
        var order = Order.Place(input.Total);
        await _orders.SaveAsync(order, cancellationToken).ConfigureAwait(false);
        return new PlaceOrderResult(order.Id);
    }

    private static IOrderRepository Fallback() => InMemoryConfig.OrderRepository(); // DCA-LAY-003: application -> infrastructure implementation
}
