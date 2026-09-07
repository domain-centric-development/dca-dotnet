using System.Collections.Generic;
using System.Linq;
using DomainCentric.ArchRules.Rules;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Rules;

public sealed class TacticalPatternRulesTests
{
    private const string Good = "DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good";
    private const string Bad = "DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad";

    private static readonly IReadOnlyDictionary<string, string> NoNegativeFixture = new Dictionary<string, string>();

    private static readonly string[] ExpectedIds = Enumerable.Range(1, 22).Select(i => $"DCA-TAC-{i:000}").ToArray();

    public static IEnumerable<object[]> RuleIds() => ExpectedIds.Select(id => new object[] { id });

    public static IEnumerable<object[]> NegativeRuleIds() =>
        ExpectedIds.Where(id => !NoNegativeFixture.ContainsKey(id)).Select(id => new object[] { id });

    private static DcaArchitecture Arch(string ns) =>
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(ns), typeof(TacticalPatternRulesTests).Assembly);

    private static IDcaRule Rule(string ns, string id) =>
        new TacticalPatternRules(DcaLayout.ForRootNamespace(ns)).Rules.Single(r => r.Id == id);

    [Fact]
    public void RuleSetHasStableShape()
    {
        var set = new TacticalPatternRules(DcaLayout.ForRootNamespace(Good));
        Assert.Equal("tactical", set.Name);
        Assert.Equal(ExpectedIds, set.Rules.Select(r => r.Id).ToArray());
        Assert.Empty(TacticalPatternRules.NotApplicable.Keys.Intersect(set.Rules.Select(r => r.Id)));
        Assert.All(set.Rules, r => Assert.False(string.IsNullOrWhiteSpace(r.Title) || string.IsNullOrWhiteSpace(r.Rationale)));
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

    /// <summary>
    /// A container of the aggregate's own type holds <em>other</em> instances of that aggregate. Only the direct
    /// member of the own type (a self-reference) is tolerated; a container never is.
    /// </summary>
    [Fact]
    public void ContainersOfTheOwnAggregateTypeAreReported()
    {
        var message = Assert.Throws<DcaRuleViolationException>(() => Rule(Bad, "DCA-TAC-003").Check(Arch(Bad))).Message;
        foreach (var container in new[] { "Children", "Siblings", "ByName", "Tree" })
        {
            Assert.Matches($"Category has field '{container}' containing .*Category", message);
        }

        Assert.DoesNotContain("'Root'", message);
        Assert.Matches("Order has field 'Customer' of type .*Customer", message);
    }
}
