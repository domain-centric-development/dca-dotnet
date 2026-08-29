using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Core.SharedKernel
{
    [SharedKernel(Description = "shared")]
    public static class SharedKernelContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Core.Cart
{
    [BoundedContext("Cart", Description = "carts")]
    [Upstream("Catalog", Translation.Conformist, Consumes.Api)]
    [Upstream("Pricing", Translation.AntiCorruptionLayer, Consumes.Events, Status = UpstreamStatus.Planned)]
    public static class CartContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Core.Cart.Domain.Model
{
    public readonly record struct CartId(System.Guid Value) : IId;

    public sealed class Cart : AggregateRootBase<Cart, CartId>
    {
        public Cart(CartId id)
        {
            Id = id;
        }

        public override CartId Id { get; }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Core.Catalog
{
    [BoundedContext("Catalog")]
    public static class CatalogContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Core.Infrastructure.Config
{
    public sealed class NoContextHere
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Core.Cart.Application.Probe
{
    public sealed class AsyncCaller
    {
        public async System.Threading.Tasks.Task RunAsync()
        {
            await System.Threading.Tasks.Task.Yield();
            _ = new Infrastructure.Config.NoContextHere();
        }
    }
}
