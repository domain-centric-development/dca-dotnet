namespace DomainCentric.ArchRules.Tests.Fixtures.NoModuleRoot.Shop.Core;

/// <summary>A model type in a layer the layout does not know by name.</summary>
public sealed class Order(string id)
{
    public string Id { get; } = id;
}
