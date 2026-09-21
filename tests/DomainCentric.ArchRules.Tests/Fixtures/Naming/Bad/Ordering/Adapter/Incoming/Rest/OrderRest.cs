using System;
using Microsoft.AspNetCore.Mvc;
using DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Application.PlaceOrder;

namespace DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Adapter.Incoming.Rest;

// DCA-NAM-006: an [ApiController] whose name does not end with the REST controller suffix ('Resource').
[ApiController]
public sealed class OrderController : ControllerBase
{
    private readonly IPlaceOrderInputPort _placeOrder;

    public OrderController(IPlaceOrderInputPort placeOrder)
    {
        _placeOrder = placeOrder;
    }

    public IPlaceOrderInputPort PlaceOrder => _placeOrder;
}

public sealed record OrderResponse(Guid OrderId);
