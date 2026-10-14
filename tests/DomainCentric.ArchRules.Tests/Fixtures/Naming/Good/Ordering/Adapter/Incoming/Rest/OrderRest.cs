using System;
using DomainCentric.ArchRules.Tests.Fixtures.Naming.Good.Ordering.Application.PlaceOrder;
using Microsoft.AspNetCore.Mvc;

namespace DomainCentric.ArchRules.Tests.Fixtures.Naming.Good.Ordering.Adapter.Incoming.Rest;

[ApiController]
public sealed class OrderResource : ControllerBase
{
    private readonly IPlaceOrderInputPort _placeOrder;

    public OrderResource(IPlaceOrderInputPort placeOrder)
    {
        _placeOrder = placeOrder;
    }

    public IPlaceOrderInputPort PlaceOrder => _placeOrder;
}

public sealed record OrderResponse(Guid OrderId);
