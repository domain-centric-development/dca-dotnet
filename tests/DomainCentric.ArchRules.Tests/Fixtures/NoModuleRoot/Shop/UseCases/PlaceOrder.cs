using DomainCentric.ArchRules.Tests.Fixtures.NoModuleRoot.Shop.Core;

namespace DomainCentric.ArchRules.Tests.Fixtures.NoModuleRoot.Shop.UseCases;

/// <summary>A use case in a layer the layout does not know by name.</summary>
public sealed class PlaceOrder
{
    public Order Execute(string id) => new(id);
}
