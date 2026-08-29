// Context-map fixture — one violation per DCA-MAP rule (001..005, 007..012).
using System;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Bad.SharedKernel
{
    [SharedKernel]
    public static class SharedKernelContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Bad.SharedKernel.Domain.Model
{
    public sealed record Money(decimal Amount, string Currency) : IValue;
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Bad.External.Payment
{
    public sealed class PaymentClient
    {
        public string Charge(decimal amount) => "ok:" + amount;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Bad.Legacy
{
    // DCA-MAP-001: context-map declaration on a namespace that is not a bounded context
    [Upstream("Catalog", Translation.Conformist, Consumes.Api, Status = UpstreamStatus.Planned)]
    public static class LegacyContext
    {
    }

    public sealed class LegacyReport
    {
        public string Title { get; } = "legacy";
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Bad.Catalog
{
    [BoundedContext("Catalog")]
    // DCA-MAP-012: partnership not mirrored by Pricing
    [Partnership("Pricing")]
    // DCA-MAP-003: two spellings normalize to the same mermaid id
    [ExternalUpstream("Payment-Service", Translation.AntiCorruptionLayer, Interaction.Outbound)]
    [ExternalUpstream("Payment Service", Translation.AntiCorruptionLayer, Interaction.Inbound)]
    public static class CatalogContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Bad.Catalog.Domain.Model
{
    public sealed class Product
    {
        public Guid Id { get; } = Guid.NewGuid();
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Bad.Catalog.Api
{
    public sealed record ProductInfo(Guid Id, string Name);
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Bad.Catalog.Events
{
    public sealed record ProductPriceChangedEvent(Guid EventId, Guid ProductId, decimal NewPrice, DateTimeOffset OccurredOn) : IIntegrationEvent;
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Bad.Catalog.Adapter.Outgoing.Pricing
{
    // DCA-MAP-011: dependency on Pricing :: Api without an [Upstream] declaration
    public sealed class PricingAdapter
    {
        public decimal Quote(Bad.Pricing.Api.PriceQuote quote) => quote.Amount;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Bad.Pricing
{
    [BoundedContext("Pricing")]
    // DCA-MAP-002: external system name is an internal context
    [ExternalUpstream("Cart", Translation.Conformist, Interaction.Inbound)]
    // DCA-MAP-004: no such context
    [Upstream("Warehouse", Translation.Conformist, Consumes.Api, Status = UpstreamStatus.Planned)]
    public static class PricingContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Bad.Pricing.Api
{
    public sealed record PriceQuote(decimal Amount);
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Bad.Pricing.Domain.Model
{
    public sealed class PriceRule
    {
        public decimal Discount { get; } = 0.1m;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Bad.Cart
{
    [BoundedContext("Cart")]
    [Upstream("Catalog", Translation.AntiCorruptionLayer, Consumes.Api)]
    // DCA-MAP-005: (Catalog, Api) declared twice
    [Upstream("Catalog", Translation.Conformist, Consumes.Api, Consumes.Events)]
    // DCA-MAP-007: Implemented, but no code depends on Pricing.Api
    [Upstream("Pricing", Translation.Conformist, Consumes.Api)]
    [ExternalUpstream("Payment Gateway", Translation.AntiCorruptionLayer, Interaction.Outbound,
        ContractNamespaces = new[] { "DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Bad.External.Payment" })]
    public static class CartContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Bad.Cart.Domain.Model
{
    // DCA-MAP-009: Conformist upstream contract type (Catalog :: Events) reaches the domain layer
    public sealed class Cart
    {
        public decimal Apply(Bad.Catalog.Events.ProductPriceChangedEvent e) => e.NewPrice;
    }

    // DCA-MAP-010: external contract type (ACL) used outside the outgoing adapter
    public sealed class Payment
    {
        private readonly Bad.External.Payment.PaymentClient _client = new();

        public string Pay(decimal amount) => _client.Charge(amount);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Bad.Cart.Application.AddToCart
{
    // DCA-MAP-008: ACL upstream contract type (Catalog :: Api) used outside the outgoing adapter
    public sealed class CatalogLookup
    {
        public string? NameOf(Bad.Catalog.Api.ProductInfo info) => info.Name;
    }
}
