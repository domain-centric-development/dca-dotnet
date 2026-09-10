// Context-map fixture — every DCA-MAP rule passes here.
using System;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Good.SharedKernel
{
    [SharedKernel]
    public static class SharedKernelContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Good.SharedKernel.Domain.Model
{
    public sealed record Money(decimal Amount, string Currency) : IValue;
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Good.External.Payment
{
    // Stand-in for a vendor SDK: an external system's contract types living outside every context.
    public sealed class PaymentClient
    {
        public string Charge(decimal amount) => "ok:" + amount;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Good.Catalog
{
    [BoundedContext("Catalog", Description = "Product catalog")]
    [Partnership("Pricing", Rationale = "Catalog and pricing evolve together")]
    public static class CatalogContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Good.Catalog.Domain.Model
{
    public sealed class Product
    {
        public Guid Id { get; } = Guid.NewGuid();
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Good.Catalog.Api
{
    public sealed record ProductInfo(Guid Id, string Name);

    [OpenHostService("Catalog")]
    public interface ICatalogApi
    {
        ProductInfo? Find(Guid id);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Good.Catalog.Events
{
    public sealed record ProductPriceChangedEvent(Guid EventId, Guid ProductId, decimal NewPrice, DateTimeOffset OccurredOn) : IIntegrationEvent;
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Good.Pricing
{
    [BoundedContext("Pricing", Description = "Price rules")]
    [Partnership("Catalog", Rationale = "Catalog and pricing evolve together")]
    public static class PricingContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Good.Pricing.Domain.Model
{
    public sealed class PriceRule
    {
        public decimal Discount { get; } = 0.1m;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Good.Cart
{
    [BoundedContext("Cart", Description = "Shopping cart")]
    [Upstream("Catalog", Translation.AntiCorruptionLayer, Consumes.Api, Rationale = "Product data is translated into cart line items")]
    [Upstream("Catalog", Translation.Conformist, Consumes.Events, Rationale = "Price change events are consumed as published")]
    [Upstream("Pricing", Translation.Conformist, Consumes.Api, Status = UpstreamStatus.Planned, Rationale = "Dynamic pricing not yet integrated")]
    [ExternalUpstream("Payment Gateway", Translation.AntiCorruptionLayer, Interaction.Outbound,
        ContractNamespaces = new[] { "DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Good.External.Payment" })]
    [ExternalUpstream("Tax Service", Translation.Conformist, Interaction.Outbound)]
    public static class CartContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Good.Cart.Domain.Model
{
    using SharedKernel.Domain.Model;

    public sealed class Cart
    {
        public Money Total { get; private set; } = new(0m, "EUR");

        public void Add(Money price) => Total = new Money(Total.Amount + price.Amount, Total.Currency);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Good.Cart.Adapter.Outgoing.Catalog
{
    using Good.Catalog.Api;

    public sealed class CatalogProductAdapter
    {
        private Good.Cart.Domain.Model.Cart localModel=null!;
        private readonly ICatalogApi _api;

        public CatalogProductAdapter(ICatalogApi api)
        {
            _api = api;
        }

        public string? NameOf(Guid id) => _api.Find(id)?.Name;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Good.Cart.Adapter.Outgoing.Payment
{
    using Good.External.Payment;

    public sealed class PaymentGatewayAdapter
    {
        private readonly PaymentClient _client = new();

        public bool Pay(decimal amount) => _client.Charge(amount).StartsWith("ok", StringComparison.Ordinal);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Good.Cart.Adapter.Incoming.Event
{
    using Good.Catalog.Events;

    public sealed class ProductPriceChangedConsumer
    {
        public decimal On(ProductPriceChangedEvent e) => e.NewPrice;
    }
}
