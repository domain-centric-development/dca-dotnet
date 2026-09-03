// Layout fixtures — the namespace shapes that context discovery must handle at any depth.
// Each shape is its own root namespace so a test can load exactly one of them.
//
// Every fixture that owns a Domain layer deliberately violates DCA-LAY-002 (domain depends on
// infrastructure), so a test can prove the layout is *governed* by breaking a rule rather than by a
// green suite: under the former one-segment wildcard these layouts matched no rule and passed silently.
using DomainCentric.BuildingBlocks.Ddd.Strategic;

// ---------------------------------------------------------------------------------------------------
// Grouped: a context two segments below the root (Root.Sales.Order)
// ---------------------------------------------------------------------------------------------------

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Grouped.Infrastructure.Config
{
    public static class GroupedConfig
    {
        public static int Capacity() => 10;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Grouped.Sales.Order
{
    [BoundedContext("Sales.Order", Description = "A context grouped below an intermediate namespace")]
    public static class OrderContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Grouped.Sales.Order.Domain.Model
{
    using Grouped.Infrastructure.Config;

    /// <summary>Depends on infrastructure, which DCA-LAY-002 forbids — the violation that proves governance.</summary>
    public sealed record Order(string Id)
    {
        public int Capacity() => GroupedConfig.Capacity();
    }
}

// ---------------------------------------------------------------------------------------------------
// Flat: a single-context application whose root namespace is the context
// ---------------------------------------------------------------------------------------------------

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Flat
{
    [BoundedContext("Flat", Description = "The root namespace itself is the context")]
    public static class FlatContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Flat.Infrastructure.Config
{
    public static class FlatConfig
    {
        public static int Capacity() => 10;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Flat.Domain.Model
{
    using Flat.Infrastructure.Config;

    public sealed record Thing(string Id)
    {
        public int Capacity() => FlatConfig.Capacity();
    }
}

// ---------------------------------------------------------------------------------------------------
// Nested: a module one level too deep that declares no [BoundedContext]
// ---------------------------------------------------------------------------------------------------

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Nested.Infrastructure.Config
{
    public static class NestedConfig
    {
        public static int Capacity() => 10;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Nested.Contexts.Todo.Domain.Model
{
    using Nested.Infrastructure.Config;

    /// <summary>
    /// Depends on infrastructure, which DCA-LAY-002 forbids. <c>Contexts.Todo</c> declares no
    /// <c>[BoundedContext]</c>; under the former one-segment wildcard it was no discovered context,
    /// DCA-LAY-002 found no domain types and passed anyway. Structural module discovery is what breaks
    /// that silence.
    /// </summary>
    public sealed record Todo(string Id)
    {
        public int Capacity() => NestedConfig.Capacity();
    }
}

// ---------------------------------------------------------------------------------------------------
// GroupedModule: a module that is deliberately not a bounded context, grouped below an intermediate namespace
// ---------------------------------------------------------------------------------------------------

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.GroupedModule.Generic.Backoffice.Application.Report
{
    /// <summary>Owns an application layer, carries no marker of any kind — and needs none.</summary>
    public sealed record BuildReportQuery(int Limit);
}

// ---------------------------------------------------------------------------------------------------
// EmptyContext: a bounded context declared before it has any code
// ---------------------------------------------------------------------------------------------------

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.EmptyContext.Planned
{
    [BoundedContext("Planned", Description = "Declared, no model yet")]
    public static class PlannedContext
    {
    }
}

// ---------------------------------------------------------------------------------------------------
// GroupedCycle: two grouped contexts whose application layers depend on each other
// ---------------------------------------------------------------------------------------------------

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.GroupedCycle.Group.Alpha
{
    [BoundedContext("Group.Alpha")]
    public static class AlphaContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.GroupedCycle.Group.Beta
{
    [BoundedContext("Group.Beta")]
    public static class BetaContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.GroupedCycle.Group.Alpha.Application.Shared
{
    using Beta.Application.Shared;

    public interface IAlphaPort
    {
        IBetaPort Beta();
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.GroupedCycle.Group.Beta.Application.Shared
{
    using Alpha.Application.Shared;

    public interface IBetaPort
    {
        IAlphaPort Alpha();
    }
}
