// Fixture for ContextMapRenderer — mirrors the Java fixture `contextmaprender`.
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMapRender.Cart
{
    [BoundedContext("Shopping Cart", Description = "Carts of guests and customers")]
    [Upstream("Catalog", Translation.AntiCorruptionLayer, Consumes.Api, Consumes.Events, Rationale = "Cart needs product master data")]
    [Partnership("Catalog", Rationale = "Catalog and cart evolve together")]
    public static class CartContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMapRender.Cart.Domain.Model
{
    public sealed record Cart(string Id);
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMapRender.Catalog
{
    [BoundedContext("Product Catalog", Description = "Master data of sellable products")]
    [Partnership("Cart", Rationale = "Catalog and cart evolve together")]
    public static class CatalogContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMapRender.Catalog.Api
{
    [OpenHostService("Catalog")]
    public interface ICatalogService
    {
        string ProductName(string productId);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMapRender.Catalog.Domain.Model
{
    public sealed record Product(string Id, string Name);
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMapRender.Shipping
{
    [BoundedContext("Shipping", Description = "Parcel dispatch")]
    [Upstream("Cart", Translation.Conformist, Consumes.Events, Status = UpstreamStatus.Planned, Rationale = "Ship what was ordered")]
    [ExternalUpstream("Carrier API", Translation.AntiCorruptionLayer, Interaction.Outbound,
        Protocol = "REST", Exchanges = "Shipment labels", Rationale = "Labels are printed by the carrier")]
    public static class ShippingContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMapRender.Shipping.Domain.Model
{
    public sealed record Shipment(string Id);
}
