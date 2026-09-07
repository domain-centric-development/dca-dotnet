using System.Collections.Generic;
using System.Linq;
using DomainCentric.ArchRules.Rules;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Rules;

/// <summary>Self-test of <see cref="LayeredRules"/> — the Hexagonal, Layered and Onion sets share one fixture tree.</summary>
public sealed class LayeredRulesTests
{
    private const string Good = "DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good";
    private const string Bad = "DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad";

    private static readonly IReadOnlyDictionary<string, string> NoNegativeFixture = new Dictionary<string, string>
    {
        ["DCA-LAY-001"] = "diagnostic rule, never fails",
        ["DCA-LAY-005"] = "checks the published building-blocks assembly, which contains only interfaces",
    };

    private static readonly string[] ExpectedIds = { "DCA-LAY-001", "DCA-LAY-002", "DCA-LAY-003", "DCA-LAY-004", "DCA-LAY-005" };

    private static DcaArchitecture Arch(string ns) =>
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(ns), typeof(LayeredRulesTests).Assembly);

    private static LayeredRules Rules(string ns) => new LayeredRules(DcaLayout.ForRootNamespace(ns));

    public static IEnumerable<object[]> RuleIds() => ExpectedIds.Select(id => new object[] { id });

    public static IEnumerable<object[]> NegativeRuleIds() =>
        ExpectedIds.Where(id => !NoNegativeFixture.ContainsKey(id)).Select(id => new object[] { id });

    [Fact]
    public void RuleSetHasStableShape()
    {
        var set = Rules(Good);
        Assert.Equal("layered", set.Name);
        Assert.Equal(ExpectedIds, set.Rules.Select(r => r.Id).ToArray());
        Assert.All(LayeredRules.NotApplicable.Keys, id => Assert.DoesNotContain(id, ExpectedIds));
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

    private const string Infra = "DomainCentric.ArchRules.Tests.Fixtures.Layout.Infra";

    private static IDcaRule InfraRule(string id) => new LayeredRules(DcaLayout.ForRootNamespace(Infra)).Rules.Single(r => r.Id == id);

    private static DcaArchitecture InfraArch() => DcaArchitecture.Load(DcaLayout.ForRootNamespace(Infra), typeof(LayeredRulesTests).Assembly);

    /// <summary>
    /// Infrastructure is selected by exact namespace: the global <c>Root.Infrastructure</c> namespace itself,
    /// every isolated module's own <c>Infrastructure</c> namespace, and never a namespace whose name merely
    /// starts with the segment.
    /// </summary>
    [Fact]
    public void UseCaseDependingOnInfrastructureAtEitherLevelIsReported()
    {
        var ex = Assert.Throws<DcaRuleViolationException>(() => InfraRule("DCA-LAY-003").Check(InfraArch()));
        Assert.Contains("GetCartUseCase", ex.Message);
        Assert.Contains("Infrastructure.Wiring", ex.Message);
        Assert.Contains("Cart.Infrastructure.CartWiring", ex.Message);
        Assert.DoesNotContain("NotInfrastructure", ex.Message);
    }

    [Fact]
    public void DomainDependingOnModuleInfrastructureIsReported()
    {
        var ex = Assert.Throws<DcaRuleViolationException>(() => InfraRule("DCA-LAY-002").Check(InfraArch()));
        Assert.Contains("CartWiring", ex.Message);
    }
}
