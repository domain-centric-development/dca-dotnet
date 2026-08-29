using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Domain.Model;

public sealed class LineItem : IEntity<LineItem, LineItemId>
{
    private Quantity _quantity;

    internal LineItem(LineItemId id, string sku, Quantity quantity)
    {
        Id = id;
        Sku = sku;
        _quantity = quantity;
    }

    public LineItemId Id { get; }

    public string Sku { get; }

    public Quantity Quantity => _quantity;

    internal void Increase(Quantity more) => _quantity = _quantity.Plus(more);
}
