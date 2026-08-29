using System.Collections.Generic;
using System.Linq;
using DomainCentric.ArchRules.Rules;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Rules;

public sealed class AdvancedPatternRulesTests
{
    private const string Good = "DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good";
    private const string Bad = "DomainCentric.ArchRules.Tests.Fixtures.Advanced.Bad";

    private static readonly string[] ExpectedIds =
    {
        "DCA-ADV-001", "DCA-ADV-002", "DCA-ADV-003", "DCA-ADV-004", "DCA-ADV-005", "DCA-ADV-006",
        "DCA-ADV-007", "DCA-ADV-008", "DCA-ADV-009", "DCA-ADV-010", "DCA-ADV-011", "DCA-ADV-012",
        "DCA-ADV-013", "DCA-ADV-014", "DCA-ADV-015", "DCA-ADV-016", "DCA-ADV-017", "DCA-ADV-018",
    };

    private static readonly IReadOnlyDictionary<string, string> NoNegativeFixture = new Dictionary<string, string>();

    private static DcaArchitecture Arch(string ns) =>
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(ns), typeof(AdvancedPatternRulesTests).Assembly);

    private static AdvancedPatternRules RuleSet(string ns) => new AdvancedPatternRules(DcaLayout.ForRootNamespace(ns));

    public static IEnumerable<object[]> RuleIds() => ExpectedIds.Select(id => new object[] { id });

    public static IEnumerable<object[]> NegativeRuleIds() =>
        ExpectedIds.Where(id => !NoNegativeFixture.ContainsKey(id)).Select(id => new object[] { id });

    [Fact]
    public void RuleSetHasStableShape()
    {
        var set = RuleSet(Good);
        Assert.Equal("advanced", set.Name);
        Assert.Equal(ExpectedIds, set.Rules.Select(r => r.Id).ToArray());
        Assert.Empty(set.Rules.Select(r => r.Id).Intersect(AdvancedPatternRules.NotApplicable.Keys));
        Assert.All(set.Rules, r => Assert.False(string.IsNullOrWhiteSpace(r.Title)));
        Assert.All(set.Rules, r => Assert.False(string.IsNullOrWhiteSpace(r.Rationale)));
    }

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void GoodFixturePasses(string id)
    {
        var rule = RuleSet(Good).Rules.Single(r => r.Id == id);
        rule.Check(Arch(Good));
    }

    [Theory]
    [MemberData(nameof(NegativeRuleIds))]
    public void BadFixtureFails(string id)
    {
        var rule = RuleSet(Bad).Rules.Single(r => r.Id == id);
        var ex = Assert.Throws<DcaRuleViolationException>(() => rule.Check(Arch(Bad)));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }
}
