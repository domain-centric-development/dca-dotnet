using System.Collections.Generic;
using System.Linq;
using DomainCentric.ArchRules.Rules;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Rules;

public sealed class NamingRulesTests
{
    private const string Good = "DomainCentric.ArchRules.Tests.Fixtures.Naming.Good";
    private const string Bad = "DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad";

    private static readonly string[] ExpectedIds =
    {
        "DCA-NAM-001",
        "DCA-NAM-003",
        "DCA-NAM-004",
        "DCA-NAM-005",
        "DCA-NAM-006",
        "DCA-NAM-007",
        "DCA-NAM-008",
        "DCA-NAM-009",
        "DCA-NAM-010",
        "DCA-NAM-011",
    };

    /// <summary>Rules without a negative fixture (id → reason).</summary>
    private static readonly IReadOnlyDictionary<string, string> NoNegativeFixture = new Dictionary<string, string>();

    public static IEnumerable<object[]> RuleIds() => ExpectedIds.Select(id => new object[] { id });

    public static IEnumerable<object[]> NegativeRuleIds() =>
        ExpectedIds.Where(id => !NoNegativeFixture.ContainsKey(id)).Select(id => new object[] { id });

    private static DcaArchitecture Arch(string ns) =>
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(ns), typeof(NamingRulesTests).Assembly);

    private static IDcaRule Rule(string ns, string id) =>
        new NamingRules(DcaLayout.ForRootNamespace(ns)).Rules.Single(r => r.Id == id);

    [Fact]
    public void RuleSetHasStableShape()
    {
        var set = new NamingRules(DcaLayout.ForRootNamespace(Good));
        Assert.Equal("naming", set.Name);
        Assert.Equal(ExpectedIds, set.Rules.Select(r => r.Id).ToArray());
        Assert.All(NamingRules.NotApplicable.Keys, id => Assert.DoesNotContain(id, ExpectedIds));
        Assert.All(set.Rules, r => Assert.False(string.IsNullOrWhiteSpace(r.Title)));
        Assert.All(set.Rules, r => Assert.False(string.IsNullOrWhiteSpace(r.Rationale)));
    }

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void GoodFixturePasses(string id) => Rule(Good, id).Check(Arch(Good));

    [Theory]
    [MemberData(nameof(NegativeRuleIds))]
    public void BadFixtureFails(string id)
    {
        var ex = Assert.Throws<DcaRuleViolationException>(() => Rule(Bad, id).Check(Arch(Bad)));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }
}
