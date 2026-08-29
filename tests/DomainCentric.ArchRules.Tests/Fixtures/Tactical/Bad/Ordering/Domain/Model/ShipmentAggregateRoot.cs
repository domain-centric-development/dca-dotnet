namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Domain.Model;

/// <summary>DCA-TAC-001: named *AggregateRoot but does not implement the marker.</summary>
public sealed class ShipmentAggregateRoot
{
    public ShipmentAggregateRoot(string id)
    {
        Id = id;
    }

    public string Id { get; }
}
