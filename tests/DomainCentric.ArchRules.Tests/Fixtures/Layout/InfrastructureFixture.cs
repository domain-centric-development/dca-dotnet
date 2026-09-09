// Infrastructure fixture — the global Infrastructure namespace itself, a module's own Infrastructure
// namespace, a namespace that merely starts with the segment, and the shared kernel's Infrastructure.
using System;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Infra.Infrastructure
{
    /// <summary>Lives directly in the global infrastructure namespace, not in a sub-namespace.</summary>
    public sealed class Wiring
    {
        public string Describe() => "wiring";
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Infra.InfrastructureX
{
    /// <summary>A namespace whose name merely starts with the infrastructure segment.</summary>
    public sealed class NotInfrastructure
    {
        public string Describe() => "fine";
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Infra.SharedKernel
{
    [SharedKernel]
    public static class SharedKernelContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Infra.SharedKernel.Infrastructure
{
    /// <summary>Shared support in the shared kernel's infrastructure namespace — everyone may use it.</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class LifecycleAttribute : Attribute
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Infra.Cart
{
    [BoundedContext("Cart")]
    public static class CartContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Infra.Cart.Infrastructure
{
    /// <summary>Per-module infrastructure.</summary>
    public sealed class CartWiring
    {
        public string Describe() => "cart wiring";
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Infra.Cart.Application.GetCart
{
    using Infra.Infrastructure;
    using Infra.InfrastructureX;
    using Infra.Cart.Infrastructure;

    /// <summary>DCA-LAY-003: depends on the global and on the module's infrastructure implementation.</summary>
    public sealed class GetCartUseCase
    {
        private readonly Wiring _wiring = new();
        private readonly CartWiring _cartWiring = new();
        private readonly NotInfrastructure _fine = new();

        public string Execute() => _wiring.Describe() + _cartWiring.Describe() + _fine.Describe();
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Infra.Cart.Adapter.Outgoing.Persistence
{
    using Infra.Cart.Infrastructure;
    using Infra.SharedKernel.Infrastructure;

    /// <summary>DCA-HEX-005 for CartWiring; the shared kernel's Lifecycle attribute is allowed.</summary>
    [Lifecycle]
    public sealed class CartStorage
    {
        private readonly Infra.Infrastructure.Wiring _global = new();
        private readonly CartWiring _wiring = new();

        public string Describe() => _wiring.Describe();
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Infra.Cart.Domain.Model
{
    using Infra.Cart.Infrastructure;

    /// <summary>DCA-LAY-002: the domain depends on the module's infrastructure.</summary>
    public sealed class Cart
    {
        private readonly CartWiring _wiring = new();

        public string Describe() => _wiring.Describe();
    }
}
