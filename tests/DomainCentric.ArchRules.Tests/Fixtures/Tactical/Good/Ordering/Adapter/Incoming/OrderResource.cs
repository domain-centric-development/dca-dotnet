using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Application.PlaceOrder;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Adapter.Incoming;

public sealed class OrderResource
{
    private readonly IPlaceOrderInputPort _placeOrder;

    public OrderResource(IPlaceOrderInputPort placeOrder)
    {
        _placeOrder = placeOrder;
    }

    public Task<PlaceOrderResult> PlaceAsync(string orderId) => _placeOrder.ExecuteAsync(new PlaceOrderCommand(orderId));
}
