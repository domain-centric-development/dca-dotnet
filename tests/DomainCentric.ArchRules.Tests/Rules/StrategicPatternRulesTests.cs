using System.Collections.Generic;
using System.Linq;
using ArchUnitNET.Loader;
using DomainCentric.ArchRules.Rules;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Rules;

public sealed class StrategicPatternRulesTests
{
    private const string Good = "DomainCentric.ArchRules.Tests.Fixtures.Strategic.Good";
    private const string Bad = "DomainCentric.ArchRules.Tests.Fixtures.Strategic.Bad";
    private const string Undeclared = "DomainCentric.ArchRules.Tests.Fixtures.Strategic.Undeclared";

    /// <summary>No type exists below this namespace: a project that holds no production code yet.</summary>
    private const string NoProductionCode = "DomainCentric.ArchRules.Tests.Fixtures.NoProductionCode";

    /// <summary>A declared context whose layers carry names the layout does not know.</summary>
    private const string NoModuleRoot = "DomainCentric.ArchRules.Tests.Fixtures.NoModuleRoot";

    private static readonly string[] ExpectedIds =
    {
        "DCA-STR-001", "DCA-STR-002", "DCA-STR-003", "DCA-STR-004", "DCA-STR-005",
        "DCA-STR-006", "DCA-STR-007", "DCA-STR-008", "DCA-STR-010",
        "DCA-STR-011", "DCA-STR-012", "DCA-STR-013",
    };

    private static readonly IReadOnlyDictionary<string, string> NoNegativeFixture = new Dictionary<string, string>
    {
        ["DCA-STR-001"] = "diagnostic rule — prints the discovered contexts, never fails",
        ["DCA-STR-010"] = "documentation-only rule — verified by code review, never fails",
        ["DCA-STR-011"] = "the bad fixture declares its contexts; absence has its own fixture",
        ["DCA-STR-012"] = "the bad fixture owns layers; their absence has its own fixture",
    };

    private static DcaArchitecture Arch(string ns) =>
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(ns), typeof(StrategicPatternRulesTests).Assembly);

    private static StrategicPatternRules Set(string ns) => new(DcaLayout.ForRootNamespace(ns));

    public static IEnumerable<object[]> RuleIds() => ExpectedIds.Select(id => new object[] { id });

    public static IEnumerable<object[]> NegativeRuleIds() =>
        ExpectedIds.Where(id => !NoNegativeFixture.ContainsKey(id)).Select(id => new object[] { id });

    [Fact]
    public void RuleSetHasStableShape()
    {
        var set = Set(Good);
        Assert.Equal("strategic", set.Name);
        Assert.Equal(ExpectedIds, set.Rules.Select(r => r.Id).ToArray());
        Assert.Empty(StrategicPatternRules.NotApplicable);
    }

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void GoodFixturePasses(string id)
    {
        var set = Set(Good);
        set.Rules.Single(r => r.Id == id).Check(Arch(Good));
    }

    [Fact]
    public void CodeBaseWithoutAnyDeclaredContextFails()
    {
        var layout = DcaLayout.ForRootNamespace(Undeclared);
        var arch = DcaArchitecture.Load(layout, typeof(StrategicPatternRulesTests).Assembly);

        var ex = Assert.Throws<DcaRuleViolationException>(
            () => StrategicPatternRules.AtLeastOneBoundedContextIsDeclared().Check(arch));

        Assert.Contains("[BoundedContext]", ex.Message);
        Assert.Contains("passes without having looked at anything", ex.Message);
    }

    [Theory]
    [MemberData(nameof(NegativeRuleIds))]
    public void BadFixtureFails(string id)
    {
        var set = Set(Bad);
        var ex = Assert.Throws<DcaRuleViolationException>(() => set.Rules.Single(r => r.Id == id).Check(Arch(Bad)));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    /// <summary>
    /// The third guard of the same kind as an empty import and an undeclared context: a layout that
    /// discovers no module root leaves every layer-selecting rule with nothing to look at, and the suite
    /// was green. This is the most likely first run of a brownfield code base.
    /// </summary>
    [Fact]
    public void CodeBaseWhoseLayersTheLayoutDoesNotKnowFails()
    {
        var layout = DcaLayout.ForRootNamespace(NoModuleRoot);
        var arch = DcaArchitecture.Load(layout, typeof(StrategicPatternRulesTests).Assembly);

        var violation = Assert.Throws<DcaRuleViolationException>(
            () => new StrategicPatternRules(layout).Rules.Single(r => r.Id == "DCA-STR-012").Check(arch));

        Assert.Contains("no module root was discovered", violation.Message, System.StringComparison.Ordinal);
        Assert.Contains("WithDomainSegment", violation.Message, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// A project without production code has nothing to declare and no module to own a layer: both guards
    /// pass, so the suite is green on an empty project. The model holds the building blocks, as Load builds
    /// it; they lie outside the root namespace and do not count as production code. The two fixtures above
    /// keep the guards red once code exists.
    /// </summary>
    [Fact]
    public void ProjectWithoutProductionCodePassesBothGuards()
    {
        var layout = DcaLayout.ForRootNamespace(NoProductionCode);
        var model = new ArchLoader().LoadAssembly(typeof(BoundedContextAttribute).Assembly).Build();
        var arch = DcaArchitecture.Of(layout, model, typeof(StrategicPatternRulesTests).Assembly);
        Assert.Empty(arch.Types);

        var rules = new StrategicPatternRules(layout).Rules;
        rules.Single(r => r.Id == "DCA-STR-011").Check(arch);
        rules.Single(r => r.Id == "DCA-STR-012").Check(arch);
    }

    [Fact]
    public void EventsInterfaceWithoutTheMarkerIsReported()
    {
        var violation = Assert.Throws<DcaRuleViolationException>(
            () => Set(Bad).Rules.Single(r => r.Id == "DCA-STR-013").Check(Arch(Bad)));
        Assert.Contains("Cart.Events.IProductRemovalTrigger", violation.Message, System.StringComparison.Ordinal);
    }
}
