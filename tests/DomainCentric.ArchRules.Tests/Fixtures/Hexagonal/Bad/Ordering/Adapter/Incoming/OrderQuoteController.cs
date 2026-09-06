using Microsoft.AspNetCore.Mvc;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Domain.Service;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.SharedKernel.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Adapter.Incoming;

/// <summary>DCA-HEX-012: an incoming adapter invoking a domain service statically instead of asking a use case.</summary>
[ApiController]
public sealed class OrderQuoteController : ControllerBase
{
    public string Quote(decimal amount) => PricingPolicy.Rounded(new Money(amount, "EUR")).ToString();
}
