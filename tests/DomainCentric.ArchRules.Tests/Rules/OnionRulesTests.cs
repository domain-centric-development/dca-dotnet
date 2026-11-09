using System.Collections.Generic;
using System.Linq;
using DomainCentric.ArchRules.Rules;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Rules;

/// <summary>Self-test of <see cref="OnionRules"/> — the Hexagonal, Layered and Onion sets share one fixture tree.</summary>
public sealed class OnionRulesTests
{
    private const string Good = "DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good";
    private const string Bad = "DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad";

    private static readonly IReadOnlyDictionary<string, string> NoNegativeFixture = new Dictionary<string, string>
    {
        ["DCA-ONI-004"] = "negative fixture in Fixtures.Asynchrony, see below",
    };

    private const string Asynchrony = "DomainCentric.ArchRules.Tests.Fixtures.Asynchrony";

    private static readonly string[] ExpectedIds = { "DCA-ONI-001", "DCA-ONI-002", "DCA-ONI-003", "DCA-ONI-004" };

    private static DcaArchitecture Arch(string ns) =>
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(ns), typeof(OnionRulesTests).Assembly);

    private static OnionRules Rules(string ns) => new OnionRules(DcaLayout.ForRootNamespace(ns));

    public static IEnumerable<object[]> RuleIds() => ExpectedIds.Select(id => new object[] { id });

    public static IEnumerable<object[]> NegativeRuleIds() =>
        ExpectedIds.Where(id => !NoNegativeFixture.ContainsKey(id)).Select(id => new object[] { id });

    [Fact]
    public void RuleSetHasStableShape()
    {
        var set = Rules(Good);
        Assert.Equal("onion", set.Name);
        Assert.Equal(ExpectedIds, set.Rules.Select(r => r.Id).ToArray());
        Assert.All(OnionRules.NotApplicable.Keys, id => Assert.DoesNotContain(id, ExpectedIds));
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

    [Fact]
    public void ASynchronousDomainPasses() =>
        Check(DcaLayout.ForRootNamespace(Asynchrony + ".Good"));

    /// <summary>A task and a cancellation token in the domain, not the application layer and not an awaitable of the code base's own.</summary>
    [Fact]
    public void AsynchronousTypesInTheDomainAreReported()
    {
        var message = Assert.Throws<DcaRuleViolationException>(() => Check(DcaLayout.ForRootNamespace(Asynchrony + ".Bad"))).Message;

        Assert.Contains("Domain.Ledger depends on System.Threading.Tasks.Task, System.Threading.Tasks.Task`1", message, System.StringComparison.Ordinal);
        Assert.Contains("Domain.PostingFeed depends on System.Threading.CancellationToken", message, System.StringComparison.Ordinal);
        Assert.DoesNotContain("RecordPostingUseCase", message, System.StringComparison.Ordinal);
        Assert.DoesNotContain("Domain.Posting depends", message, System.StringComparison.Ordinal);
    }

    [Fact]
    public void TheAsynchronousTypesAreConfigurable()
    {
        var ns = Asynchrony + ".Bad";
        var own = DcaLayout.ForRootNamespace(ns).WithAsynchronousTypes(ns + ".Ledger.Domain.Deferred");

        var message = Assert.Throws<DcaRuleViolationException>(() => Check(own)).Message;

        Assert.Contains("Domain.Posting depends", message, System.StringComparison.Ordinal);
        Assert.DoesNotContain("Domain.Ledger depends", message, System.StringComparison.Ordinal);
        Check(DcaLayout.ForRootNamespace(ns).WithAsynchronousTypes());
    }

    private static void Check(DcaLayout layout) =>
        new OnionRules(layout).Rules.Single(r => r.Id == "DCA-ONI-004")
            .Check(DcaArchitecture.Load(layout, typeof(OnionRulesTests).Assembly));
}

