using System.Collections.Generic;
using System.Linq;
using DomainCentric.ArchRules.Rules;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Rules;

public sealed class UseCaseRulesTests
{
    private const string Good = "DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good";
    private const string Bad = "DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad";

    private static readonly string[] ExpectedIds =
    {
        "DCA-USE-001",
        "DCA-USE-002",
        "DCA-USE-003",
        "DCA-USE-004",
        "DCA-USE-005",
        "DCA-USE-006",
        "DCA-USE-007",
        "DCA-USE-008",
        "DCA-USE-009",
        "DCA-USE-010",
        "DCA-USE-011",
        "DCA-USE-014",
    };

    /// <summary>Rules without a negative fixture (id → reason).</summary>
    private static readonly IReadOnlyDictionary<string, string> NoNegativeFixture = new Dictionary<string, string>();

    public static IEnumerable<object[]> RuleIds() => ExpectedIds.Select(id => new object[] { id });

    public static IEnumerable<object[]> NegativeRuleIds() =>
        ExpectedIds.Where(id => !NoNegativeFixture.ContainsKey(id)).Select(id => new object[] { id });

    private static DcaArchitecture Arch(string ns) =>
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(ns), typeof(UseCaseRulesTests).Assembly);

    private static IDcaRule Rule(string ns, string id) =>
        new UseCaseRules(DcaLayout.ForRootNamespace(ns)).Rules.Single(r => r.Id == id);

    [Fact]
    public void RuleSetHasStableShape()
    {
        var set = new UseCaseRules(DcaLayout.ForRootNamespace(Good));
        Assert.Equal("usecase", set.Name);
        Assert.Equal(ExpectedIds, set.Rules.Select(r => r.Id).ToArray());
        Assert.All(UseCaseRules.NotApplicable.Keys, id => Assert.DoesNotContain(id, ExpectedIds));
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
