using System;
using System.Linq;
using Xunit;
using Layer = DomainCentric.ArchRules.DcaLayout.Layer;
using Source = DomainCentric.ArchRules.DcaArchitecture.PlacementSource;

namespace DomainCentric.ArchRules.Tests;

/// <summary>
/// A project that declares some of its layers with its own attributes — an assembly attribute naming a namespace,
/// inherited by the namespaces below it, an attribute on a single type, or one on the whole assembly — beside the
/// path the library defaults to.
/// </summary>
public sealed class LayerMarkerTests
{
    private const string App = "DomainCentric.ArchRules.Tests.Fixtures.LayerAttributes.App";
    private const string Billing = App + ".Billing";
    private const string Vocabulary = "DomainCentric.ArchRules.Tests.Fixtures.LayerAttributes.Vocabulary.";

    private static DcaLayout NamespaceAndAttributes() =>
        DcaLayout.ForRootNamespace(App)
            .WithOutgoingAdapter(DcaMarker.AnyOf(
                DcaMarker.OfNamespace("Adapter.Outgoing"),
                DcaMarker.OfAttribute(Vocabulary + "OutgoingLayerAttribute")))
            .WithIncomingAdapter(DcaMarker.AnyOf(
                DcaMarker.OfNamespace("Adapter.Incoming"),
                DcaMarker.OfAttribute(Vocabulary + "IncomingLayerAttribute")))
            .WithDomain(DcaMarker.AnyOf(
                DcaMarker.OfNamespace("Domain"),
                DcaMarker.OfAttribute(Vocabulary + "DomainLayerAttribute")));

    private static DcaArchitecture Arch(DcaLayout layout) =>
        DcaArchitecture.Load(layout, typeof(LayerMarkerTests).Assembly);

    private static DcaArchitecture.Placement? PlacementOf(DcaArchitecture arch, string typeName) =>
        arch.PlacementOf(arch.TypeNamed(typeName) ?? throw new InvalidOperationException("no such type: " + typeName));

    [Fact]
    public void WithoutTheAttributesTheDeclaredNamespaceIsInNoLayer()
    {
        var arch = Arch(DcaLayout.ForRootNamespace(App));

        Assert.Null(PlacementOf(arch, Billing + ".Out.Persistence.JdbcInvoiceStore"));
        Assert.Equal(Layer.OutgoingAdapter, PlacementOf(arch, Billing + ".Adapter.Outgoing.Legacy.LegacyHook")!.Layer);
        Assert.Empty(arch.LayerNotices());
    }

    [Fact]
    public void ANamespaceDeclarationHoldsForItsSubtree()
    {
        var placement = PlacementOf(Arch(NamespaceAndAttributes()), Billing + ".Out.Persistence.JdbcInvoiceStore")!;

        Assert.Equal(Layer.OutgoingAdapter, placement.Layer);
        Assert.Equal(Billing + ".Out", placement.LayerNamespace);
        Assert.Equal(Source.Namespace, placement.Source);
    }

    [Fact]
    public void ATypeAttributePlacesTheTypeWhereverItSits()
    {
        var placement = PlacementOf(Arch(NamespaceAndAttributes()), Billing + ".Lone.MailGateway")!;

        Assert.Equal(Layer.OutgoingAdapter, placement.Layer);
        Assert.Equal(Source.Type, placement.Source);
    }

    [Fact]
    public void TheAttributeWinsOverThePath()
    {
        var arch = Arch(NamespaceAndAttributes());

        Assert.Equal(Layer.IncomingAdapter, PlacementOf(arch, Billing + ".Adapter.Outgoing.Legacy.LegacyHook")!.Layer);
        Assert.Equal(Source.Path, PlacementOf(arch, Billing + ".Adapter.Incoming.Web.InvoicePage")!.Source);
        Assert.Equal(Layer.Domain, PlacementOf(arch, Billing + ".Domain.Model.Invoice")!.Layer);
    }

    [Fact]
    public void TheNearestNamespaceDeclarationWins()
    {
        var arch = Arch(NamespaceAndAttributes());

        var inner = PlacementOf(arch, Billing + ".Conflict.Inner.Odd")!;
        Assert.Equal(Layer.Domain, inner.Layer);
        Assert.Equal(Billing + ".Conflict.Inner", inner.LayerNamespace);
    }

    [Fact]
    public void TheLayerPredicatesAnswerFromThePlacement()
    {
        var arch = Arch(NamespaceAndAttributes());
        var store = arch.TypeNamed(Billing + ".Out.Persistence.JdbcInvoiceStore")!;

        Assert.True(arch.InOutgoingAdapter(store));
        Assert.True(arch.InAdapter(store));
        Assert.True(arch.InLayerOf(Layer.OutgoingAdapter, Billing)(store));
        Assert.False(arch.InDomain(store));
        Assert.Contains(Billing, arch.ModuleRoots());
    }

    [Fact]
    public void AnOverrideAndAContradictingSubnamespaceAreNoticed()
    {
        var notices = Arch(NamespaceAndAttributes()).LayerNotices();

        Assert.Contains(
            "namespace " + Billing + ".Adapter.Outgoing.Legacy: path says outgoingAdapter, attribute says incomingAdapter,"
                + " incomingAdapter holds",
            notices);
        Assert.Contains(
            "namespace " + Billing + ".Conflict.Inner declares domain inside " + Billing
                + ".Conflict, which declares outgoingAdapter - one layer per subtree",
            notices);
    }

    [Fact]
    public void ALayerGivenOnlyByAttributeHasNoPath()
    {
        var layout = DcaLayout.ForRootNamespace(App)
            .WithIncomingAdapter(DcaMarker.OfAttribute(Vocabulary + "IncomingLayerAttribute"));
        var arch = Arch(layout);

        Assert.False(layout.LayerHasNamespace(Layer.IncomingAdapter));
        Assert.Null(PlacementOf(arch, Billing + ".Adapter.Incoming.Web.InvoicePage"));
        Assert.Equal(Layer.IncomingAdapter, PlacementOf(arch, Billing + ".Adapter.Outgoing.Legacy.LegacyHook")!.Layer);
    }

    /// <summary>.NET decision (a): an attribute on the assembly that names no namespace places every type of it.</summary>
    [Fact]
    public void AnAssemblyAttributePlacesEveryTypeOfTheAssembly()
    {
        var layout = DcaLayout.ForRootNamespace(App)
            .WithApplication(DcaMarker.AnyOf(
                DcaMarker.OfNamespace("Application"),
                DcaMarker.OfAttribute(Vocabulary + "AssemblyLayerAttribute")));
        var arch = Arch(layout);

        var invoice = PlacementOf(arch, Billing + ".Domain.Model.Invoice")!;
        Assert.Equal(Layer.Application, invoice.Layer);
        Assert.Equal(Source.Assembly, invoice.Source);
        Assert.Equal(Billing, invoice.LayerNamespace);
        Assert.Contains(
            arch.LayerNotices(),
            n => n.StartsWith("namespace " + Billing + ".Domain.Model: path says domain, attribute says application on assembly ", StringComparison.Ordinal));
    }

    /// <summary>.NET decision (a) + (b): the namespace an assembly attribute names is nearer than the whole assembly.</summary>
    [Fact]
    public void ANamespaceDeclarationIsNearerThanTheAssembly()
    {
        var layout = DcaLayout.ForRootNamespace(App)
            .WithApplication(DcaMarker.OfAttribute(Vocabulary + "AssemblyLayerAttribute"))
            .WithIncomingAdapter(DcaMarker.AnyOf(
                DcaMarker.OfNamespace("Adapter.Incoming"),
                DcaMarker.OfAttribute(Vocabulary + "IncomingLayerAttribute")));
        var arch = Arch(layout);

        var hook = PlacementOf(arch, Billing + ".Adapter.Outgoing.Legacy.LegacyHook")!;
        Assert.Equal(Layer.IncomingAdapter, hook.Layer);
        Assert.Equal(Source.Namespace, hook.Source);
        Assert.Contains(
            "namespace " + Billing + ".Adapter.Outgoing.Legacy declares incomingAdapter inside assembly DomainCentric.ArchRules.Tests,"
                + " which declares application - one layer per subtree",
            arch.LayerNotices());
    }

    [Fact]
    public void TheReportNamesTheLayersRecognisedOtherwise()
    {
        Assert.Equal("namespace (library default)", DcaLayout.ForRootNamespace(App).LayersReport());
        Assert.Equal(
            "outgoingAdapter=AnyOf(OfNamespace(Adapter.Outgoing), OfAttribute(" + Vocabulary + "OutgoingLayerAttribute))",
            DcaLayout.ForRootNamespace(App)
                .WithOutgoingAdapter(DcaMarker.AnyOf(
                    DcaMarker.OfNamespace("Adapter.Outgoing"),
                    DcaMarker.OfAttribute(Vocabulary + "OutgoingLayerAttribute")))
                .LayersReport());
    }

    [Fact]
    public void ALayerTakesNoTypeAndAnAdapterNamespaceStaysBelowTheAdapterSegment()
    {
        var layout = DcaLayout.ForRootNamespace(App);

        var type = Assert.Throws<ArgumentException>(() => layout.WithDomain(DcaMarker.OfType("Acme.DomainThing")));
        Assert.Contains("OfNamespace or OfAttribute", type.Message, StringComparison.Ordinal);
        var adapter = Assert.Throws<ArgumentException>(() => layout.WithOutgoingAdapter(DcaMarker.OfNamespace("Secondary")));
        Assert.Contains("WithAdapterSegment", adapter.Message, StringComparison.Ordinal);
        Assert.Equal("Out", layout.WithOutgoingAdapter(DcaMarker.OfNamespace("Adapter.Out")).OutgoingSegment);
    }

    [Fact]
    public void AnyOfFlattensAndAOneMemberAnyOfIsThatMember()
    {
        var attribute = DcaMarker.OfAttribute("Acme.LayerAttribute");

        Assert.Equal(attribute, DcaMarker.AnyOf(attribute));
        Assert.Equal(
            3,
            DcaMarker.AnyOf(
                    DcaMarker.AnyOf(DcaMarker.OfNamespace("Domain"), attribute),
                    DcaMarker.OfAttribute("Example.LayerAttribute"))
                .Alternatives.Count);
    }

    [Fact]
    public void TheDefaultLayoutPlacesByPathAlone()
    {
        var arch = Arch(DcaLayout.ForRootNamespace(App));

        Assert.Equal(Source.Path, PlacementOf(arch, Billing + ".Domain.Model.Invoice")!.Source);
        Assert.True(arch.InDomainModel(arch.TypeNamed(Billing + ".Domain.Model.Invoice")!));
        Assert.True(arch.InSharedOutputPorts(arch.TypeNamed(Billing + ".Application.Shared.IMailPort")!));
        Assert.Null(PlacementOf(arch, Billing + ".Lone.MailGateway"));
    }
}
