using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace Acme.Shop.Cart.Domain.Model;

public readonly record struct CartId(Guid Value) : IId
{
    public static CartId Generate() => new(Guid.NewGuid());
}
