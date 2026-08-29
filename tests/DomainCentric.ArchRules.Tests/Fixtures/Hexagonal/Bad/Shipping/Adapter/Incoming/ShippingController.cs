using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Application.PlaceOrder;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.SharedKernel.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Shipping.Adapter.Incoming;

/// <summary>DCA-HEX-007: an incoming adapter of Shipping orchestrating a use case of Order.</summary>
public sealed class ShippingController : ControllerBase
{
    private readonly IPlaceOrderInputPort _placeOrder;

    public ShippingController(IPlaceOrderInputPort placeOrder)
    {
        _placeOrder = placeOrder;
    }

    public Task<PlaceOrderResult> Reorder(CancellationToken cancellationToken) =>
        _placeOrder.ExecuteAsync(new PlaceOrderCommand(new Money(1, "EUR")), cancellationToken);
}
