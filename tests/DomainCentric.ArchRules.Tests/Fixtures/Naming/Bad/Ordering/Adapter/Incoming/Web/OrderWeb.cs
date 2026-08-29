using System;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Application.PlaceOrder;

namespace DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Adapter.Incoming.Web;

public sealed class OrderPageController : PageModel
{
    private readonly IPlaceOrderInputPort _placeOrder;

    public OrderPageController(IPlaceOrderInputPort placeOrder)
    {
        _placeOrder = placeOrder;
    }

    public IPlaceOrderInputPort PlaceOrder => _placeOrder;
}

public sealed record OrderDto(Guid OrderId);

public sealed record OrderViewModel(string OrderId);

public static class OrderConverter
{
    public static OrderViewModel ToViewModel(OrderDto dto) => new(dto.OrderId.ToString());
}
