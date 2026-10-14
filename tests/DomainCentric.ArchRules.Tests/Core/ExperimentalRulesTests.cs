using System;
using System.Collections.Generic;
using System.Linq;
using DomainCentric.ArchRules.Xunit;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Core;

/// <summary>
/// Rule maturity: an experimental rule runs only where the selection opted in, and the xUnit carrier
/// reports it in a group of its own. The rules here are test fixtures, not catalog entries.
/// </summary>
public sealed class ExperimentalRulesTests
{
    private const string Set = "usecase";
    private const string ReleasedId = "DCA-USE-901";
    private const string CatalogId = "DCA-USE-001";

    private static readonly IDcaRule Released = Fixture(ReleasedId, experimental: false);
    private static readonly IDcaRule Trial = Fixture("DCA-USE-900", experimental: true);

    private static IDcaRule Fixture(string id, bool experimental)
    {
        var rule = DcaRule.Check(id, "fixture rule", "test", _ => { });
        return (experimental ? rule.Experimental() : rule).Selecting("test").Checking("test");
    }

    private static IReadOnlyList<IDcaRuleSet> FixtureSets() => new IDcaRuleSet[] { new FixtureSet(Set, new[] { Released, Trial }) };

    private static IReadOnlyDictionary<string, string> Properties(params (string Key, string Value)[] entries) =>
        entries.ToDictionary(e => e.Key, e => e.Value, StringComparer.Ordinal);

    /// <summary>The catalog's experimental rules — every other rule of the catalog is released.</summary>
    private static readonly string[] CatalogExperimentalIds =
    {
        "DCA-ERR-007", "DCA-HEX-014", "DCA-HEX-015", "DCA-HEX-016", "DCA-MAP-014", "DCA-NAM-012", "DCA-NAM-013", "DCA-TAC-024", "DCA-USE-018", "DCA-USE-019", "DCA-USE-020",
    };

    [Fact]
    public void TheCatalogsExperimentalRulesAreExactlyTheDeclaredOnes() =>
        Assert.Equal(
            CatalogExperimentalIds,
            DcaRules.All(DcaLayout.ForRootNamespace("Acme.Shop"))
                .Where(rule => rule.Maturity == DcaRuleMaturity.Experimental)
                .Select(rule => rule.Id)
                .OrderBy(id => id, StringComparer.Ordinal));

    [Fact]
    public void ARuleIsReleasedUnlessDeclaredExperimental()
    {
        Assert.Equal(DcaRuleMaturity.Released, Released.Maturity);
        Assert.Equal(DcaRuleMaturity.Experimental, Trial.Maturity);
        Assert.Equal(DcaRuleKind.Enforced, Trial.Kind);
    }

    [Fact]
    public void TheDefaultSelectionLeavesAnExperimentalRuleOut()
    {
        var selection = DcaRuleSelection.All();

        Assert.False(selection.IncludesExperimental);
        Assert.False(selection.Includes(Set, Trial));
        Assert.True(selection.Includes(Set, Released));
    }

    [Fact]
    public void WithExperimentalIncludesAnExperimentalRule()
    {
        var selection = DcaRuleSelection.All().WithExperimental();

        Assert.True(selection.IncludesExperimental);
        Assert.True(selection.Includes(Set, Trial));
        Assert.True(selection.Includes(Set, Released));
    }

    [Fact]
    public void WithExperimentalComposesWithScopeAndSeverity()
    {
        var selection = DcaRuleSelection.All()
            .OnlySets("tactical")
            .WithExperimental()
            .Warning(CatalogId, "on trial");

        Assert.False(selection.Includes(Set, Trial), "scope still applies to an experimental rule");
        Assert.True(selection.IncludesExperimental);
        Assert.Equal(DcaSeverity.Warn, selection.SeverityOf(CatalogId));
        Assert.True(DcaRuleSelection.All().WithExperimental().OnlySets(Set).Includes(Set, Trial));
    }

    [Fact]
    public void MergingKeepsAnOptInFromEitherSide()
    {
        Assert.True(DcaRuleSelection.All().WithExperimental().MergedWith(DcaRuleSelection.All()).IncludesExperimental);
        Assert.True(DcaRuleSelection.All().MergedWith(DcaRuleSelection.All().WithExperimental()).IncludesExperimental);
    }

    [Fact]
    public void TheConfigurationKeyIncludesAnExperimentalRule()
    {
        var selection = DcaRuleSelection.FromProperties(Properties((DcaRuleSelection.ExperimentalKey, "true")));

        Assert.Equal("dca.rules.experimental", DcaRuleSelection.ExperimentalKey);
        Assert.True(selection.Includes(Set, Trial));
        Assert.False(DcaRuleSelection.FromProperties(Properties((DcaRuleSelection.ExperimentalKey, "false"))).Includes(Set, Trial));
    }

    [Fact]
    public void TheConfigurationKeySetToFalseLeavesItOut()
    {
        var selection = DcaRuleSelection.FromProperties(Properties((DcaRuleSelection.ExperimentalKey, "false")));

        Assert.False(selection.IncludesExperimental);
        Assert.False(selection.Includes(Set, Trial));
        Assert.True(selection.Includes(Set, Released));
    }

    [Theory]
    [InlineData("TRUE", true)]
    [InlineData("True", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void TheConfigurationKeyIgnoresCaseAndTreatsBlankAsNoOptIn(string value, bool optedIn)
    {
        var selection = DcaRuleSelection.FromProperties(Properties((DcaRuleSelection.ExperimentalKey, value)));

        Assert.Equal(optedIn, selection.IncludesExperimental);
        Assert.Equal(optedIn, selection.Includes(Set, Trial));
    }

    [Fact]
    public void TheConfigurationKeyRejectsAValueThatIsNotABoolean()
    {
        var rejected = Assert.Throws<ArgumentException>(
            () => DcaRuleSelection.FromProperties(Properties((DcaRuleSelection.ExperimentalKey, "yes"))));

        Assert.Contains(DcaRuleSelection.ExperimentalKey, rejected.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnOptedInExperimentalRuleIsReportedInTheExperimentalGroup()
    {
        var names = DcaArchitectureTest.CaseNames(FixtureSets(), DcaRuleSelection.All().WithExperimental());

        Assert.Equal(new[] { $"{Set} / {ReleasedId}", "experimental (1) / DCA-USE-900" }, names);
    }

    [Fact]
    public void AnExperimentalRuleWithoutOptInIsNamedAsSuch()
    {
        var names = DcaArchitectureTest.CaseNames(FixtureSets(), DcaRuleSelection.All());

        Assert.Equal(new[] { $"{Set} / {ReleasedId}", "experimental (1) / DCA-USE-900 [NOT OPTED IN]" }, names);
    }

    [Fact]
    public void AReleasedRuleKeepsItsSetGroupAndSeverity()
    {
        var sets = new IDcaRuleSet[] { new FixtureSet(Set, new[] { Fixture(CatalogId, experimental: false), Trial }) };
        var names = DcaArchitectureTest.CaseNames(
            sets, DcaRuleSelection.All().WithExperimental().Excluding(CatalogId, "not here"));

        Assert.Equal(new[] { $"{Set} / {CatalogId} [OFF: not here]", "experimental (1) / DCA-USE-900" }, names);
    }

    [Fact]
    public void TheSummaryCountsExperimentalRulesApart()
    {
        var informational = DcaRule.Informational("DCA-USE-902", "fixture", "test", _ => Array.Empty<string>())
            .Selecting("test").Checking("test");

        Assert.Equal("1 enforced, 1 informational, 1 experimental", DcaArchitectureTest.Summary(new[] { Released, informational, Trial }));
    }

    [Fact]
    public void ExecutionRunsAnExperimentalRuleLikeAnyOther()
    {
        var failing = DcaRule.Check("DCA-USE-903", "fixture", "test", _ => DcaRule.Fail("fixture", new[] { "violation" }))
            .Experimental().Selecting("test").Checking("test");
        var arch = DcaArchitecture.Load(
            DcaLayout.ForRootNamespace("DomainCentric.ArchRules.Tests.Fixtures.Core"), typeof(ExperimentalRulesTests).Assembly);

        var outcome = DcaRuleExecution.Execute(failing, arch, DcaRuleSelection.All().WithExperimental());

        Assert.Equal(DcaRuleStatus.Failed, outcome.Status);
    }

    private sealed record FixtureSet(string Name, IReadOnlyList<IDcaRule> Rules) : IDcaRuleSet;
}
