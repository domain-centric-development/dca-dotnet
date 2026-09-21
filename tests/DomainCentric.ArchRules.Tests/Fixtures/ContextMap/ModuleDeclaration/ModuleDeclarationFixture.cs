// DCA-MAP-006: a project that declares its module boundaries with an attribute of its own. No .NET
// preset names such an attribute, so the rule is inert until ModuleDeclarationAttributeTypes points
// at one.
using System;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.ModuleDeclaration
{
    /// <summary>A module system's per-module declaration, as a project might define it.</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ApplicationModuleAttribute : Attribute
    {
        public string[] AllowedDependencies { get; set; } = Array.Empty<string>();
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.ModuleDeclaration.Catalog
{
    [BoundedContext("Catalog", Description = "Product catalog")]
    [ApplicationModule]
    public static class CatalogContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.ModuleDeclaration.Ordering
{
    // The context map says Ordering consumes Catalog's Api; the module declaration allows its Events
    // instead. Neither side may know more than the other.
    [BoundedContext("Ordering", Description = "Order placement")]
    [ApplicationModule(AllowedDependencies = new[] { "Catalog :: Events" })]
    [Upstream("Catalog", Translation.AntiCorruptionLayer, Consumes.Api, Rationale = "reads the catalogue")]
    public static class OrderingContext
    {
    }
}
