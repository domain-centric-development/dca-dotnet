namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Undeclared.Ordering.Domain.Model;

/// <summary>A module that owns a domain layer while no namespace below the root declares a context.</summary>
public sealed class Order
{
    public Order(string id) => Id = id;

    public string Id { get; }
}
