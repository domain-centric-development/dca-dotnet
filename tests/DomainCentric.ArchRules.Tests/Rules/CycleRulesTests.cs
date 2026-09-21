using System.Collections.Generic;
using System.Linq;
using DomainCentric.ArchRules.Rules;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Rules;

public sealed class CycleRulesTests
{
    private const string Good = "DomainCentric.ArchRules.Tests.Fixtures.Cycles.Good";
    private const string Bad = "DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad";

    /// <summary>
    /// One rule since 0.5.0. DCA-CYC-001 to DCA-CYC-004 sliced per module root, so the only cycle they
    /// could report ran between two modules — and its first direction is already forbidden outright by
    /// DCA-STR-004, DCA-STR-003, DCA-STR-006 and DCA-HEX-007. DCA-CYC-005 slices within a module and is
    /// the one that reports something no sibling does.
    /// </summary>
    private static readonly string[] ExpectedIds = { "DCA-CYC-005" };

    /// <summary>Rules without a negative fixture (id → reason).</summary>
    private static readonly IReadOnlyDictionary<string, string> NoNegativeFixture = new Dictionary<string, string>();

    public static IEnumerable<object[]> RuleIds() => ExpectedIds.Select(id => new object[] { id });

    public static IEnumerable<object[]> NegativeRuleIds() =>
        ExpectedIds.Where(id => !NoNegativeFixture.ContainsKey(id)).Select(id => new object[] { id });

    private static DcaArchitecture Arch(string ns) =>
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(ns), typeof(CycleRulesTests).Assembly);

    private static IDcaRule Rule(string ns, string id) =>
        new CycleRules(DcaLayout.ForRootNamespace(ns)).Rules.Single(r => r.Id == id);

    [Fact]
    public void RuleSetHasStableShape()
    {
        var set = new CycleRules(DcaLayout.ForRootNamespace(Good));
        Assert.Equal("cycles", set.Name);
        Assert.Equal(ExpectedIds, set.Rules.Select(r => r.Id).ToArray());
        Assert.All(CycleRules.NotApplicable.Keys, id => Assert.DoesNotContain(id, ExpectedIds));
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
