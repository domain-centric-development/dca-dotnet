using System.Collections.Generic;
using System.Linq;
using DomainCentric.ArchRules.Rules;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Rules;

/// <summary>Self-test of <see cref="HexagonalRules"/> — the Hexagonal, Layered and Onion sets share one fixture tree.</summary>
public sealed class HexagonalRulesTests
{
    private const string Good = "DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good";
    private const string Bad = "DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad";

    private static readonly IReadOnlyDictionary<string, string> NoNegativeFixture = new Dictionary<string, string>
    {
        ["DCA-HEX-016"] = "informational diagnostic, never fails - EnumMemberNamesAreObservedOnTheBadFixtureOnly shows it",
        ["DCA-HEX-017"] = "its roles are empty in every preset - ListenersThatDriveAnInputPort* configure them on the Listeners fixture",
        ["DCA-HEX-018"] = "negative fixture in ControllerPlace - AControllerOutsideTheIncomingAdaptersIsReported",

    };

    private static readonly string[] ExpectedIds = { "DCA-HEX-001", "DCA-HEX-002", "DCA-HEX-003", "DCA-HEX-004", "DCA-HEX-005", "DCA-HEX-006", "DCA-HEX-007", "DCA-HEX-008", "DCA-HEX-009", "DCA-HEX-010", "DCA-HEX-011", "DCA-HEX-012", "DCA-HEX-013", "DCA-HEX-014", "DCA-HEX-016", "DCA-HEX-017", "DCA-HEX-018" };

    private static DcaArchitecture Arch(string ns) =>
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(ns), typeof(HexagonalRulesTests).Assembly);

    private static HexagonalRules Rules(string ns) => new HexagonalRules(DcaLayout.ForRootNamespace(ns));

    public static IEnumerable<object[]> RuleIds() => ExpectedIds.Select(id => new object[] { id });

    public static IEnumerable<object[]> NegativeRuleIds() =>
        ExpectedIds.Where(id => !NoNegativeFixture.ContainsKey(id)).Select(id => new object[] { id });

    [Fact]
    public void RuleSetHasStableShape()
    {
        var set = Rules(Good);
        Assert.Equal("hexagonal", set.Name);
        Assert.Equal(ExpectedIds, set.Rules.Select(r => r.Id).ToArray());
        Assert.All(HexagonalRules.NotApplicable.Keys, id => Assert.DoesNotContain(id, ExpectedIds));
        Assert.All(set.Rules, r => Assert.False(string.IsNullOrWhiteSpace(r.Title)));
        Assert.All(set.Rules, r => Assert.False(string.IsNullOrWhiteSpace(r.Rationale)));
    }

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void GoodFixturePasses(string id)
    {
        var rule = Rules(Good).Rules.Single(r => r.Id == id);
        rule.Check(Arch(Good));
    }

    [Theory]
    [MemberData(nameof(NegativeRuleIds))]
    public void BadFixtureFails(string id)
    {
        var rule = Rules(Bad).Rules.Single(r => r.Id == id);
        var ex = Assert.Throws<DcaRuleViolationException>(() => rule.Check(Arch(Bad)));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    private static DcaLayout ListenerLayout(string ns) => DcaLayout.ForRootNamespace(ns).WithFrameworkTypes(
        FrameworkTypes.AspNetCore() with
        {
            EventListenerTypes = new[] { ns + ".Infrastructure.Events.IEventListener" },
            TransactionalEventListenerTypes = new[] { ns + ".Infrastructure.Events.ITransactionalEventListener" },
        });

    private static void CheckListeners(string ns)
    {
        var layout = ListenerLayout(ns);
        new HexagonalRules(layout).Rules.Single(r => r.Id == "DCA-HEX-017")
            .Check(DcaArchitecture.Load(layout, typeof(HexagonalRulesTests).Assembly));
    }

    [Fact]
    public void ASynchronousListenerThatDrivesAnInputPortIsReported()
    {
        const string ns = "DomainCentric.ArchRules.Tests.Fixtures.Listeners.Bad";
        var message = Assert.Throws<DcaRuleViolationException>(() => CheckListeners(ns)).Message;

        Assert.Contains("Listeners.Bad.Ledger.Adapter.Incoming.Event.PostingNotifier drives IUseCase.ExecuteAsync inside the transaction that raised the event", message);
    }

    [Fact]
    public void ATransactionalListenerAndASynchronousTranslatorPass() =>
        CheckListeners("DomainCentric.ArchRules.Tests.Fixtures.Listeners.Good");

    [Fact]
    public void TheListenerRolesAreEmptyInEveryPreset()
    {
        Assert.Empty(FrameworkTypes.AspNetCore().EventListenerTypes);
        Assert.Empty(FrameworkTypes.AspNetCore().TransactionalEventListenerTypes);
        Assert.Empty(FrameworkTypes.None().TransactionalEventListenerTypes);
    }

    private static void CheckControllerPlace(string ns)
    {
        var layout = DcaLayout.ForRootNamespace(ns);
        new HexagonalRules(layout).Rules.Single(r => r.Id == "DCA-HEX-018")
            .Check(DcaArchitecture.Load(layout, typeof(HexagonalRulesTests).Assembly));
    }

    [Fact]
    public void AControllerOutsideTheIncomingAdaptersIsReported()
    {
        var message = Assert.Throws<DcaRuleViolationException>(
            () => CheckControllerPlace("DomainCentric.ArchRules.Tests.Fixtures.ControllerPlace.Bad")).Message;

        Assert.Contains("Application.Shared.LedgerController is a controller outside the incoming adapters", message);
        Assert.Contains("Infrastructure.LedgerResource is a controller outside the incoming adapters", message);
    }

    [Fact]
    public void ControllersInTheIncomingAdaptersPass() =>
        CheckControllerPlace("DomainCentric.ArchRules.Tests.Fixtures.ControllerPlace.Good");

    [Fact]
    public void DomainServiceRuleNamesInjectedAndStaticUse()
    {
        var ex = Assert.Throws<DcaRuleViolationException>(() => Rules(Bad).Rules.Single(r => r.Id == "DCA-HEX-012").Check(Arch(Bad)));
        Assert.Contains("OrderController depends on the domain service", ex.Message);
        Assert.Contains("OrderQuoteController depends on the domain service", ex.Message);
    }

    [Fact]
    public void InputPortRuleNamesTheOutgoingAdapter()
    {
        var ex = Assert.Throws<DcaRuleViolationException>(() => Rules(Bad).Rules.Single(r => r.Id == "DCA-HEX-013").Check(Arch(Bad)));
        Assert.Contains("ReorderingAdapter depends on the input port", ex.Message);
    }

    private const string Infra = "DomainCentric.ArchRules.Tests.Fixtures.Layout.Infra";

    /// <summary>A module's own Infrastructure namespace counts; the shared kernel's is shared support.</summary>
    [Fact]
    public void OutgoingAdapterMayReuseOwnAndGlobalInfrastructure()
    {
        var rule = new HexagonalRules(DcaLayout.ForRootNamespace(Infra)).Rules.Single(r => r.Id == "DCA-HEX-005");
        var arch = DcaArchitecture.Load(DcaLayout.ForRootNamespace(Infra), typeof(HexagonalRulesTests).Assembly);
        rule.Check(arch);
    }

    [Fact]
    public void GetHandlersThatDriveACommandAreReportedByAttributeAndByName()
    {
        var message = Assert.Throws<DcaRuleViolationException>(() => Rules(Bad).Rules.Single(r => r.Id == "DCA-HEX-014").Check(Arch(Bad))).Message;

        Assert.Contains("OrderLinkResource.Reorder handles GET and drives IUseCase.ExecuteAsync with the command PlaceOrderCommand", message);
        Assert.Contains("ReorderPage.OnGetAsync handles GET", message);
    }

    [Fact]
    public void AGeneralVerbAttributeRestrictedToGetIsSelected()
    {
        var message = Assert.Throws<DcaRuleViolationException>(() => Rules(Bad).Rules.Single(r => r.Id == "DCA-HEX-014").Check(Arch(Bad))).Message;

        Assert.Contains("OrderLinkResource.Repeat handles GET and drives", message);
    }

    [Fact]
    public void TheGetMappingRoleTakesAnAttributeEntry()
    {
        Assert.Contains("Microsoft.AspNetCore.Mvc.AcceptVerbsAttribute#HttpMethods=GET", FrameworkTypes.AspNetCore().GetMappingAttributeTypes);
        Assert.Throws<System.ArgumentException>(() => FrameworkTypes.None() with { GetMappingAttributeTypes = new[] { "X.MappingAttribute#HttpMethods" } });
    }

    [Fact]
    public void AnActionThatNamesNoHttpMethodIsAGetHandler()
    {
        var message = Assert.Throws<DcaRuleViolationException>(() => Rules(Bad).Rules.Single(r => r.Id == "DCA-HEX-014").Check(Arch(Bad))).Message;

        Assert.Contains("OrderLinkResource.Again handles GET and drives", message);
        Assert.Contains("OrderLinkResource.Renew handles GET and drives", message);
        Assert.DoesNotContain("ReorderPage.Preview", message);
    }

    [Fact]
    public void TheGetMappingRoleTakesAnActionThatNamesNoHttpMethod()
    {
        Assert.Contains("Microsoft.AspNetCore.Mvc.Routing.IActionHttpMethodProvider#HttpMethods=", FrameworkTypes.AspNetCore().GetMappingAttributeTypes);
        Assert.Contains("Microsoft.AspNetCore.Mvc.NonActionAttribute", FrameworkTypes.AspNetCore().NonActionAttributeTypes);
        Assert.Empty(FrameworkTypes.None().NonActionAttributeTypes);
    }

    [Fact]
    public void WithoutGetRolesNoHandlerIsSelected()
    {
        var layout = DcaLayout.ForRootNamespace(Bad).WithFrameworkTypes(FrameworkTypes.AspNetCore() with
        {
            GetMappingAttributeTypes = System.Array.Empty<string>(),
            GetHandlerNamePrefixes = System.Array.Empty<string>(),
        });

        new HexagonalRules(layout).Rules.Single(r => r.Id == "DCA-HEX-014").Check(DcaArchitecture.Load(layout, typeof(HexagonalRulesTests).Assembly));
    }

    [Fact]
    public void EnumMemberNamesAreObservedOnTheBadFixtureOnly()
    {
        Assert.Empty(Rules(Good).Rules.Single(r => r.Id == "DCA-HEX-016").Observe(Arch(Good)));

        var observed = string.Join("\n", Rules(Bad).Rules.Single(r => r.Id == "DCA-HEX-016").Observe(Arch(Bad)));
        Assert.Contains("OrderStatusText.Plain calls ToString on the enum", observed);
        Assert.Contains("OrderStatusText.Formatted calls ToString on the enum", observed);
        Assert.Contains("OrderStatusText.Interpolated interpolates the enum", observed);
        // The compiler turns concatenation into a constrained ToString; composite formatting boxes the value.
        Assert.Contains("OrderStatusText.Concatenated calls ToString on the enum", observed);
        Assert.Contains("OrderStatusText.Composite formats into text (String.Format) the enum", observed);
        Assert.Contains("OrderStatusText.Named calls Enum.GetName on the enum", observed);
    }
}
