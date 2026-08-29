using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Domain.Model;

/// <summary>
/// DCA-TAC-004 (no IId-typed member — the non-generic IEntity does not force one), DCA-TAC-005 (public
/// constructor), DCA-TAC-006 (inherited public setter), DCA-TAC-007 (field of aggregate-root type).
/// </summary>
public sealed class Shipment : TrackedEntity, IEntity
{
    private readonly Order _order;

    public Shipment(string id, Order order)
    {
        Id = id;
        _order = order;
    }

    public string Id { get; }

    public Order Order => _order;
}
