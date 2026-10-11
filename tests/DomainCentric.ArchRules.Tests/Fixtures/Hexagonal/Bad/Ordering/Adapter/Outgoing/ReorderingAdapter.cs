using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Application.PlaceOrder;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Adapter.Outgoing;

/// <summary>DCA-HEX-013: an outgoing adapter that drives the application through an input port.</summary>
public sealed class ReorderingAdapter
{
    private readonly IPlaceOrderInputPort _placeOrder;

    public ReorderingAdapter(IPlaceOrderInputPort placeOrder)
    {
        _placeOrder = placeOrder;
    }
}
