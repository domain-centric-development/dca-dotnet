using System.Collections.Generic;
using System.Linq;
using DomainCentric.ArchRules.Rules;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Rules;

public sealed class DotnetRulesTests
{
    private const string Good = "DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Good";
    private const string Bad = "DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Bad";

    private const string OwnContract = "DomainCentric.ArchRules.Tests.Fixtures.Dotnet.OwnContract";

    private static readonly IReadOnlyDictionary<string, string> NoNegativeFixture = new Dictionary<string, string>
    {
        // With the library's own vocabulary the C# compiler enforces the signature of
        // IUseCase<TIn,TOut>, so the rule can only fail where the use-case role names another
        // contract — see UseCaseContractOfAProjectsOwnVocabularyIsChecked.
        ["DCA-NET-003"] = "the default contract cannot violate it; the OwnContract fixture does",
    };

    private static readonly string[] ExpectedIds =
    {
        "DCA-NET-001", "DCA-NET-002", "DCA-NET-003", "DCA-NET-004", "DCA-NET-005", "DCA-NET-006",
    };

    private static DcaArchitecture Arch(string ns) =>
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(ns), typeof(DotnetRulesTests).Assembly);

    private static DotnetRules Set(string ns) => new(DcaLayout.ForRootNamespace(ns));

    public static IEnumerable<object[]> RuleIds() => ExpectedIds.Select(id => new object[] { id });

    public static IEnumerable<object[]> NegativeRuleIds() =>
        ExpectedIds.Where(id => !NoNegativeFixture.ContainsKey(id)).Select(id => new object[] { id });

    [Fact]
    public void RuleSetHasStableShape()
    {
        var set = Set(Good);
        Assert.Equal("dotnet", set.Name);
        Assert.Equal(ExpectedIds, set.Rules.Select(r => r.Id).ToArray());
        Assert.Empty(DotnetRules.NotApplicable);
        Assert.All(set.Rules, r => Assert.False(string.IsNullOrWhiteSpace(r.Rationale)));
    }

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void GoodFixturePasses(string id)
    {
        var rule = Set(Good).Rules.Single(r => r.Id == id);
        rule.Check(Arch(Good));
    }

    [Theory]
    [MemberData(nameof(NegativeRuleIds))]
    public void BadFixtureFails(string id)
    {
        var rule = Set(Bad).Rules.Single(r => r.Id == id);
        var ex = Assert.Throws<DcaRuleViolationException>(() => rule.Check(Arch(Bad)));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    /// <summary>
    /// DCA-NET-003 reads the configured use-case role, not the name of a method. Pointing the role at
    /// a project's own contract is the only way it can be violated — the library's own contract is
    /// enforced by the compiler.
    /// </summary>
    [Fact]
    public void UseCaseContractOfAProjectsOwnVocabularyIsChecked()
    {
        var layout = DcaLayout.ForRootNamespace(OwnContract).WithMarkers(DcaMarkers.Dca() with
        {
            UseCase = OwnContract + ".Vocabulary.IHandler",
        });
        var arch = DcaArchitecture.Load(layout, typeof(DotnetRulesTests).Assembly);
        var rule = new DotnetRules(layout).Rules.Single(r => r.Id == "DCA-NET-003");

        var message = Assert.Throws<DcaRuleViolationException>(() => rule.Check(arch)).Message;

        Assert.Contains("ShipParcelUseCase.Handle", message, System.StringComparison.Ordinal);
        Assert.Contains("CancellationToken", message, System.StringComparison.Ordinal);
    }
}
