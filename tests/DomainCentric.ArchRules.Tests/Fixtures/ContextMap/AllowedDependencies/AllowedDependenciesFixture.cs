// DCA-MAP-014 fixtures: a module declaration whose allowed dependencies the code does or does not use.

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Good
{
    /// <summary>A module system's per-module declaration, as a project might define it - inside the scanned root.</summary>
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class ModuleAttribute : System.Attribute
    {
        public string[] AllowedDependencies { get; set; } = System.Array.Empty<string>();
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Good.Catalog
{
    [DomainCentric.BuildingBlocks.Ddd.Strategic.BoundedContext("Catalog")]
    [DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Good.Module]
    public static class CatalogContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Good.Catalog.Api
{
    public sealed record ItemView(string Name);
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Good.Billing
{
    [DomainCentric.BuildingBlocks.Ddd.Strategic.BoundedContext("Billing")]
    [DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Good.Module]
    public static class BillingContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Good.Billing.Api
{
    public sealed record InvoiceView(decimal Total);
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Good.Ordering
{
    // Allows exactly what the code uses.
    [DomainCentric.BuildingBlocks.Ddd.Strategic.BoundedContext("Ordering")]
    [DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Good.Module(AllowedDependencies = new[] { "Catalog :: Api" })]
    public static class OrderingContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Good.Ordering.Adapter.Outgoing.Catalog
{
    public sealed class ItemLookup
    {
        public string NameOf(DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Good.Catalog.Api.ItemView item) => item.Name;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Bad
{
    /// <summary>A module system's per-module declaration, as a project might define it - inside the scanned root.</summary>
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class ModuleAttribute : System.Attribute
    {
        public string[] AllowedDependencies { get; set; } = System.Array.Empty<string>();
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Bad.Catalog
{
    [DomainCentric.BuildingBlocks.Ddd.Strategic.BoundedContext("Catalog")]
    [DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Bad.Module]
    public static class CatalogContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Bad.Catalog.Api
{
    public sealed record ItemView(string Name);
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Bad.Billing
{
    [DomainCentric.BuildingBlocks.Ddd.Strategic.BoundedContext("Billing")]
    [DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Bad.Module]
    public static class BillingContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Bad.Billing.Api
{
    public sealed record InvoiceView(decimal Total);
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Bad.Ordering
{
    // Allows Billing, which no type of Ordering uses.
    [DomainCentric.BuildingBlocks.Ddd.Strategic.BoundedContext("Ordering")]
    [DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Bad.Module(AllowedDependencies = new[] { "Catalog :: Api", "Billing" })]
    public static class OrderingContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Bad.Ordering.Adapter.Outgoing.Catalog
{
    public sealed class ItemLookup
    {
        public string NameOf(DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies.Bad.Catalog.Api.ItemView item) => item.Name;
    }
}
