using System;
using System.Linq;
using Xunit;

namespace DomainCentric.ArchRules.Tests;

/// <summary>
/// A project that marks its building blocks and its commands with attributes of its own instead of supertypes or
/// names.
/// </summary>
public sealed class RoleMarkerTests
{
    private const string App = "DomainCentric.ArchRules.Tests.Fixtures.RoleAttributes.App";
    private const string Model = App + ".Orders.Domain.Model.";
    private const string Vocabulary = "DomainCentric.ArchRules.Tests.Fixtures.RoleAttributes.Vocabulary.";

    private static DcaLayout Attributed() =>
        DcaLayout.ForRootNamespace(App)
            .WithMarkers((DcaMarkers.Dca() with { Name = "attributed" })
                .WithValue(DcaMarker.OfAttribute(Vocabulary + "ValueObjectAttribute"))
                .WithCommand(DcaMarker.OfAttribute(Vocabulary + "CommandAttribute")));

    private static DcaArchitecture Arch(DcaLayout layout) =>
        DcaArchitecture.Load(layout, typeof(RoleMarkerTests).Assembly);

    private static ArchUnitNET.Domain.IType Type(DcaArchitecture arch, string simpleName) =>
        arch.TypeNamed(Model + simpleName) ?? throw new InvalidOperationException("no such type: " + simpleName);

    private static IDcaRule Rule(DcaLayout layout, string id) => DcaRules.All(layout).Single(r => r.Id == id);

    [Fact]
    public void AnAttributeRecognisesABuildingBlock()
    {
        var arch = Arch(Attributed());
        var markers = arch.Layout.Markers;

        Assert.True(markers.Is("Value", Type(arch, "Amount")));
        Assert.False(markers.Is("Value", Type(arch, "Order")));
        Assert.False(DcaMarkers.Dca().Is("Value", Type(arch, "Amount")));
        Assert.True(markers.Is("Value", typeof(Fixtures.RoleAttributes.App.Orders.Domain.Model.Amount)));
    }

    [Fact]
    public void AnAttributedOneFieldValueIsAWrapperForTheBarePrimitiveRule()
    {
        var layout = Attributed();
        Rule(layout, "DCA-TAC-025").Check(Arch(layout));

        var unmarked = DcaLayout.ForRootNamespace(App);
        var error = Assert.Throws<DcaRuleViolationException>(() => Rule(unmarked, "DCA-TAC-025").Check(Arch(unmarked)));
        Assert.Contains(Model + "Amount", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CommandsAreRecognisedBySuffixByDefaultAndByTheProjectsAttributeOnceConfigured()
    {
        var byDefault = Arch(DcaLayout.ForRootNamespace(App));
        var byAttribute = Arch(Attributed());

        Assert.True(byDefault.Layout.Markers.Is("Command", Type(byDefault, "ShowOrderCommand")));
        Assert.False(byDefault.Layout.Markers.Is("Command", Type(byDefault, "PlaceOrderRequest")));
        Assert.True(byAttribute.Layout.Markers.Is("Command", Type(byAttribute, "PlaceOrderRequest")));
        Assert.False(byAttribute.Layout.Markers.Is("Command", Type(byAttribute, "ShowOrderCommand")));
    }

    [Fact]
    public void TheCommandRuleFollowsTheConfiguredRole()
    {
        var layout = Attributed();
        var error = Assert.Throws<DcaRuleViolationException>(() => Rule(layout, "DCA-USE-002").Check(Arch(layout)));

        Assert.Contains("PlaceOrderRequest", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowOrderCommand", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ABuildingBlockTakesNoNamespaceAndNoSuffix()
    {
        var markers = DcaMarkers.Dca();

        var byNamespace = Assert.Throws<ArgumentException>(() => markers.WithValue(DcaMarker.OfNamespace("Domain.Model.Values")));
        Assert.Contains("not what it is", byNamespace.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => markers.WithValue(DcaMarker.OfSuffix("Value")));
        Assert.Throws<ArgumentException>(() => markers.WithBoundedContext(DcaMarker.OfType("Acme.Context")));
        Assert.Throws<ArgumentException>(() => markers.WithRepository(
            DcaMarker.AnyOf(DcaMarker.OfType("Acme.IA"), DcaMarker.OfType("Acme.IB"))));
    }

    [Fact]
    public void APlainTypeStaysTheRolesTypeAndTheReportNamesTheRest()
    {
        var markers = DcaMarkers.Dca()
            .WithAggregateRoot(DcaMarker.OfType("Acme.IRoot"))
            .WithValue(DcaMarker.AnyOf(DcaMarker.OfType("Acme.IValue"), DcaMarker.OfAttribute("Acme.ValueObjectAttribute")));

        Assert.Equal("Acme.IRoot", markers.AggregateRoot);
        Assert.Equal("Acme.IValue", markers.Value);
        Assert.Equal(DcaMarker.OfType("Acme.IRoot"), markers.Marker("AggregateRoot"));
        Assert.DoesNotContain(markers.RecognisedOtherwise(), r => r.Key == "AggregateRoot");
        Assert.Equal(
            "dca (AggregateRoot=Acme.IRoot, Value=AnyOf(OfType(Acme.IValue), OfAttribute(Acme.ValueObjectAttribute)))",
            DcaLayout.ForRootNamespace(App).WithMarkers(markers).MarkersReport());
        Assert.True(DcaMarkers.Dca().IsDefault);
        Assert.False(DcaMarkers.Dca().WithQuery(DcaMarker.OfSuffix("Read")).IsDefault);
    }

    [Fact]
    public void TheBarePrimitiveRulePassesOverFailuresAndAValuesOwnErrorShapesAndExplainsAOneFieldType()
    {
        const string app = "DomainCentric.ArchRules.Tests.Fixtures.BareValues.App";
        const string model = app + ".Orders.Domain.Model.";
        var layout = DcaLayout.ForRootNamespace(app);
        var message = Assert.Throws<DcaRuleViolationException>(() => Rule(layout, "DCA-TAC-025").Check(Arch(layout))).Message;

        Assert.DoesNotContain("OrderRefused", message, StringComparison.Ordinal);
        Assert.DoesNotContain("EmptyError", message, StringComparison.Ordinal);
        Assert.DoesNotContain("TooLongError", message, StringComparison.Ordinal);
        Assert.DoesNotContain(model + "PostalCode:", message, StringComparison.Ordinal);
        Assert.Contains(
            model + "Street: property Name is String - it has one field: mark it as a value or an id"
                + " (OfType(DomainCentric.BuildingBlocks.Ddd.Tactical.IValue)) to count as a wrapper",
            message,
            StringComparison.Ordinal);
    }
}
