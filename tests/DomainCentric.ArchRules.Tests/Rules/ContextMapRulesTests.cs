using System.Collections.Generic;
using System.Linq;
using DomainCentric.ArchRules.Rules;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Rules;

public sealed class ContextMapRulesTests
{
    private const string Good = "DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Good";
    private const string Bad = "DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Bad";

    private static readonly string[] ExpectedIds =
    {
        "DCA-MAP-001", "DCA-MAP-002", "DCA-MAP-003", "DCA-MAP-004", "DCA-MAP-005",
        "DCA-MAP-007", "DCA-MAP-008", "DCA-MAP-009", "DCA-MAP-010", "DCA-MAP-011",
        "DCA-MAP-012", "DCA-MAP-013",
    };

    private static readonly IReadOnlyDictionary<string, string> NoNegativeFixture = new Dictionary<string, string>
    {
        ["DCA-MAP-013"] = "diagnostic rule — prints the declared context map, never fails",
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
        Assert.Equal(new[] { "DCA-MAP-006" }, ContextMapRules.NotApplicable.Keys.ToArray());
        Assert.DoesNotContain(set.Rules, r => ContextMapRules.NotApplicable.ContainsKey(r.Id));
    }

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void GoodFixturePasses(string id)
    {
        var set = Set(Good);
        set.Rules.Single(r => r.Id == id).Check(Arch(Good));
    }

    [Theory]
    [MemberData(nameof(NegativeRuleIds))]
    public void BadFixtureFails(string id)
    {
        var set = Set(Bad);
        var ex = Assert.Throws<DcaRuleViolationException>(() => set.Rules.Single(r => r.Id == id).Check(Arch(Bad)));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }
}
