using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace Acme.Shop.SharedKernel.Domain.Model;

public readonly record struct ProductId(Guid Value) : IId
{
    public static ProductId Generate() => new(Guid.NewGuid());
}
