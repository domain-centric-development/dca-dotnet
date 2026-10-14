using System;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.PlaceOrder;
using Microsoft.AspNetCore.Mvc;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Adapter.Incoming.Rest;

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
