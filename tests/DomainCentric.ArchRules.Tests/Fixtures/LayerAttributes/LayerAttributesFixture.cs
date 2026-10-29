// Layer-attributes fixture — a project that declares some of its layers with its own attributes: as an assembly
// attribute naming a namespace (held for the namespace and everything below it), on a single type, or on the whole
// assembly, beside the path the library defaults to. The attributes are the test's own; the library ships none.
using System;
using DomainCentric.ArchRules.Tests.Fixtures.LayerAttributes;
using DomainCentric.ArchRules.Tests.Fixtures.LayerAttributes.Vocabulary;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

// The project's outgoing adapters, declared by namespace; the path names no layer there.
[assembly: OutgoingLayer(LayerAttributesNamespaces.Billing + ".Out")]

// The path says outgoing adapter, the attribute incoming - the attribute holds.
[assembly: IncomingLayer(LayerAttributesNamespaces.Billing + ".Adapter.Outgoing.Legacy")]

// A namespace that declares another layer than the namespace above it - a notice.
[assembly: OutgoingLayer(LayerAttributesNamespaces.Billing + ".Conflict")]
[assembly: DomainLayer(LayerAttributesNamespaces.Billing + ".Conflict.Inner")]

// An attribute naming no namespace: it places every type of the assembly a layout names it for.
[assembly: AssemblyLayer]

namespace DomainCentric.ArchRules.Tests.Fixtures.LayerAttributes
{
    internal static class LayerAttributesNamespaces
    {
        public const string App = "DomainCentric.ArchRules.Tests.Fixtures.LayerAttributes.App";
        public const string Billing = App + ".Billing";
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.LayerAttributes.Vocabulary
{
    /// <summary>A project's own layer attribute: on a type, or on the assembly naming a namespace.</summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public sealed class DomainLayerAttribute : Attribute
    {
        public DomainLayerAttribute()
        {
        }

        public DomainLayerAttribute(string ns) => Namespace = ns;

        public string? Namespace { get; }
    }

    /// <summary>A project's own layer attribute: on a type, or on the assembly naming a namespace.</summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public sealed class IncomingLayerAttribute : Attribute
    {
        public IncomingLayerAttribute()
        {
        }

        public IncomingLayerAttribute(string ns) => Namespace = ns;

        public string? Namespace { get; }
    }

    /// <summary>A project's own layer attribute: on a type, or on the assembly naming a namespace.</summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public sealed class OutgoingLayerAttribute : Attribute
    {
        public OutgoingLayerAttribute()
        {
        }

        public OutgoingLayerAttribute(string ns) => Namespace = ns;

        public string? Namespace { get; }
    }

    /// <summary>A layer attribute for a whole assembly - a project with one assembly per layer.</summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class AssemblyLayerAttribute : Attribute
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.LayerAttributes.App.Billing
{
    [BoundedContext("Billing")]
    public static class BillingContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.LayerAttributes.App.Billing.Adapter.Incoming.Web
{
    /// <summary>Incoming by its path.</summary>
    public sealed class InvoicePage
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.LayerAttributes.App.Billing.Adapter.Outgoing.Legacy
{
    public sealed class LegacyHook
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.LayerAttributes.App.Billing.Application.Shared
{
    public interface IInvoiceStore : IOutputPort
    {
    }

    public interface IMailPort : IOutputPort
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.LayerAttributes.App.Billing.Conflict.Inner
{
    public sealed class Odd
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.LayerAttributes.App.Billing.Domain.Model
{
    /// <summary>In the domain by its path.</summary>
    public sealed class Invoice
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.LayerAttributes.App.Billing.Lone
{
    using DomainCentric.ArchRules.Tests.Fixtures.LayerAttributes.App.Billing.Application.Shared;

    /// <summary>Outgoing by its own attribute, in a namespace that declares nothing.</summary>
    [OutgoingLayer]
    public sealed class MailGateway : IMailPort
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.LayerAttributes.App.Billing.Out.Persistence
{
    using DomainCentric.ArchRules.Tests.Fixtures.LayerAttributes.App.Billing.Application.Shared;

    /// <summary>Outgoing by the declaration of a namespace above it.</summary>
    public sealed class JdbcInvoiceStore : IInvoiceStore
    {
    }
}
