using System;
using System.Collections.Generic;
using System.Linq;
using DomainCentric.ArchRules.Rules;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Rules;

public sealed class ContextMapRulesTests
{
    private const string Good = "DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Good";
    private const string Bad = "DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Bad";
    private const string Nested = "DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Nested";
    private const string Planned = "DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Planned";
    private const string PlannedMisplaced = "DomainCentric.ArchRules.Tests.Fixtures.ContextMap.PlannedMisplaced";
    private const string ModuleDeclaration = "DomainCentric.ArchRules.Tests.Fixtures.ContextMap.ModuleDeclaration";

    private static readonly string[] ExpectedIds =
    {
        "DCA-MAP-001", "DCA-MAP-002", "DCA-MAP-004", "DCA-MAP-005", "DCA-MAP-006",
        "DCA-MAP-007", "DCA-MAP-008", "DCA-MAP-009", "DCA-MAP-010", "DCA-MAP-011",
        "DCA-MAP-012", "DCA-MAP-013", "DCA-MAP-014",
    };

    private static readonly IReadOnlyDictionary<string, string> NoNegativeFixture = new Dictionary<string, string>
    {
        ["DCA-MAP-013"] = "diagnostic rule — prints the declared context map, never fails",
        // Inert until a project configures a module-declaration attribute — no .NET preset names
        // one. Its negative fixture therefore needs a configured layout: see
        // AModuleDeclarationThatDisagreesWithTheContextMapIsReportedOnceConfigured.
        ["DCA-MAP-006"] = "no .NET preset configures a module-declaration attribute; the ModuleDeclaration fixture does",
        ["DCA-MAP-014"] = "no .NET preset configures a module-declaration attribute; the AllowedDependencies fixture does",
    };

    private static DcaArchitecture Arch(string ns) =>
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(ns), typeof(ContextMapRulesTests).Assembly);

    private static ContextMapRules Set(string ns) => new(DcaLayout.ForRootNamespace(ns));

    public static IEnumerable<object[]> RuleIds() => ExpectedIds.Select(id => new object[] { id });

    public static IEnumerable<object[]> NegativeRuleIds() =>
        ExpectedIds.Where(id => !NoNegativeFixture.ContainsKey(id)).Select(id => new object[] { id });

    [Fact]
    public void RuleSetHasStableShape()
    {
        var set = Set(Good);
        Assert.Equal("contextmap", set.Name);
        Assert.Equal(ExpectedIds, set.Rules.Select(r => r.Id).ToArray());
        Assert.Empty(ContextMapRules.NotApplicable);
        Assert.DoesNotContain(set.Rules, r => ContextMapRules.NotApplicable.ContainsKey(r.Id));
    }

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void GoodFixturePasses(string id)
    {
        var set = Set(Good);
        set.Rules.Single(r => r.Id == id).Check(Arch(Good));
    }

    /// <summary>A Planned declaration without code passes MAP-007..011: nothing is demanded.</summary>
    [Theory]
    [InlineData("DCA-MAP-007")]
    [InlineData("DCA-MAP-008")]
    [InlineData("DCA-MAP-009")]
    [InlineData("DCA-MAP-010")]
    [InlineData("DCA-MAP-011")]
    public void PlannedDeclarationsWithoutCodeDemandNothing(string id) => Set(Planned).Rules.Single(r => r.Id == id).Check(Arch(Planned));

    /// <summary>A Planned declaration does not exempt existing code from placement; only MAP-008's translation-site demand waits.</summary>
    [Theory]
    [InlineData("DCA-MAP-008")]
    [InlineData("DCA-MAP-009")]
    [InlineData("DCA-MAP-010")]
    public void PlannedDeclarationsStillGovernPlacement(string id)
    {
        Set(PlannedMisplaced).Rules.Single(r => r.Id == "DCA-MAP-007").Check(Arch(PlannedMisplaced));
        var ex = Assert.Throws<DcaRuleViolationException>(() => Set(PlannedMisplaced).Rules.Single(r => r.Id == id).Check(Arch(PlannedMisplaced)));
        Assert.Contains("Invoice", ex.Message, System.StringComparison.Ordinal);
        Assert.DoesNotContain("needs translation evidence", ex.Message, System.StringComparison.Ordinal);
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
    /// A relationship is a statement of the context, so the namespace that carries it must be the context
    /// root. A declaration on a nested namespace is reported — it would otherwise be silently ignored by
    /// every other context-map rule and by the renderer.
    /// </summary>
    [Fact]
    public void DeclarationOnNestedNamespaceIsReported()
    {
        var ex = Assert.Throws<DcaRuleViolationException>(() => Set(Nested).Rules.Single(r => r.Id == "DCA-MAP-001").Check(Arch(Nested)));
        Assert.Contains(Nested + ".Cart.Application.GetCart", ex.Message);
        Assert.Contains("[Partnership]", ex.Message);
    }

    /// <summary>
    /// DCA-MAP-006 is carried under the same id as in Java and selects nothing until a project names
    /// its module-declaration attribute — .NET draws module boundaries with projects, so no preset
    /// names one. Java behaves identically where no module system is on the class path.
    /// </summary>
    [Fact]
    public void AModuleDeclarationThatDisagreesWithTheContextMapIsReportedOnceConfigured()
    {
        Set(ModuleDeclaration).Rules.Single(r => r.Id == "DCA-MAP-006").Check(Arch(ModuleDeclaration));

        const string attribute = ModuleDeclaration + ".ApplicationModuleAttribute";
        var layout = DcaLayout.ForRootNamespace(ModuleDeclaration).WithFrameworkTypes(
            FrameworkTypes.AspNetCore() with { ModuleDeclarationAttributeTypes = new[] { attribute } });
        var rule = new ContextMapRules(layout).Rules.Single(r => r.Id == "DCA-MAP-006");

        var message = Assert.Throws<DcaRuleViolationException>(
            () => rule.Check(DcaArchitecture.Load(layout, typeof(ContextMapRulesTests).Assembly))).Message;

        Assert.Contains("Ordering", message, StringComparison.Ordinal);
        Assert.Contains("Catalog :: Api", message, StringComparison.Ordinal);
        Assert.Contains("Catalog :: Events", message, StringComparison.Ordinal);
    }

    /// <summary>
    /// DCA-MAP-014 reads the same configured module-declaration attribute as DCA-MAP-006 and selects nothing
    /// without one. As in Java, an entry a Planned upstream announces and an entry naming no module of the scan
    /// are skipped; 'module :: *' means the whole module.
    /// </summary>
    [Fact]
    public void AnAllowedDependencyNoCodeUsesIsReportedOnceConfigured()
    {
        const string root = "DomainCentric.ArchRules.Tests.Fixtures.ContextMap.AllowedDependencies";
        IDcaRule Rule(DcaLayout layout) => new ContextMapRules(layout).Rules.Single(r => r.Id == "DCA-MAP-014");
        DcaLayout Configured(string ns) => DcaLayout.ForRootNamespace(ns).WithFrameworkTypes(
            FrameworkTypes.AspNetCore() with { ModuleDeclarationAttributeTypes = new[] { ns + ".ModuleAttribute" } });

        var unconfigured = DcaLayout.ForRootNamespace(root + ".Bad");
        Rule(unconfigured).Check(DcaArchitecture.Load(unconfigured, typeof(ContextMapRulesTests).Assembly));

        var good = Configured(root + ".Good");
        Rule(good).Check(DcaArchitecture.Load(good, typeof(ContextMapRulesTests).Assembly));

        var bad = Configured(root + ".Bad");
        var message = Assert.Throws<DcaRuleViolationException>(
            () => Rule(bad).Check(DcaArchitecture.Load(bad, typeof(ContextMapRulesTests).Assembly))).Message;
        Assert.Contains("Context 'Ordering': the module declaration allows 'Billing'", message, StringComparison.Ordinal);
        Assert.Contains("Context 'Ordering': the module declaration allows 'Common'", message, StringComparison.Ordinal);
        Assert.DoesNotContain("'Catalog :: Api'", message, StringComparison.Ordinal);
    }
}
