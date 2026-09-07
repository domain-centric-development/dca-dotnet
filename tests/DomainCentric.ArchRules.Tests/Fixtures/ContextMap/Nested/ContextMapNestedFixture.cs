// Context-map fixture — a relationship declared below the context root (DCA-MAP-001).
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Nested.Cart
{
    [BoundedContext("Cart")]
    public static class CartContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Nested.Cart.Application.GetCart
{
    // DCA-MAP-001: a relationship declared on a use-case namespace, not on the context root
    [Partnership("Catalog")]
    public static class GetCartFeature
    {
    }

    public sealed class GetCartUseCase
    {
        public string Execute() => "cart";
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Nested.Catalog
{
    [BoundedContext("Catalog")]
    [Partnership("Cart")]
    public static class CatalogContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Nested.Catalog.Domain.Model
{
    public sealed record Product(string Sku);
}
