using System;
using Microsoft.AspNetCore.Mvc;
using DomainCentric.ArchRules.Tests.Fixtures.Naming.Good.Ordering.Application.PlaceOrder;

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
