using DomainCentric.BuildingBlocks.Ddd.Strategic;

namespace DomainCentric.ArchRules.Tests.Fixtures.NoModuleRoot.Shop;

/// <summary>A declared context whose layers carry names the layout does not know.</summary>
[BoundedContext("shop")]
public sealed class ShopContext;
