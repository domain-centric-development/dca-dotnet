// Role-attributes fixture — a project that marks its building blocks and its commands with attributes of its own
// instead of supertypes or names. The attributes are the test's own; the library ships none.
using System;
using DomainCentric.ArchRules.Tests.Fixtures.RoleAttributes.Vocabulary;
using DomainCentric.BuildingBlocks.Ddd.Strategic;

namespace DomainCentric.ArchRules.Tests.Fixtures.RoleAttributes.Vocabulary
{
    /// <summary>A project's own role attribute, on a type.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class ValueObjectAttribute : Attribute
    {
    }

    /// <summary>A project's own role attribute, on a type.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class CommandAttribute : Attribute
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.RoleAttributes.App.Orders
{
    [BoundedContext("Orders")]
    public static class OrdersContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.RoleAttributes.App.Orders.Domain.Model
{
    /// <summary>A value by the project's attribute, a wrapper of one component.</summary>
    [ValueObject]
    public sealed record Amount(long Cents);

    /// <summary>Holds values only.</summary>
    public sealed class Order
    {
        public Order(Amount total) => Total = total;

        public Amount Total { get; }
    }

    /// <summary>A command by the project's attribute, misplaced in the domain.</summary>
    [Command]
    public sealed record PlaceOrderRequest(Amount Total);

    /// <summary>Named like a command, but the project marks its commands by attribute - a read.</summary>
    public sealed record ShowOrderCommand(Amount Total);
}
