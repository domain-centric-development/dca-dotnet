using Microsoft.AspNetCore.Mvc;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Shipping.Adapter.Incoming;

public sealed class ShippingController : ControllerBase
{
    public string Status() => "shipped";
}
