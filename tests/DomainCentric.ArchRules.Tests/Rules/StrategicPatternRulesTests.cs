using System.Collections.Generic;
using System.Linq;
using DomainCentric.ArchRules.Rules;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Rules;

public sealed class StrategicPatternRulesTests
{
    private const string Good = "DomainCentric.ArchRules.Tests.Fixtures.Strategic.Good";
    private const string Bad = "DomainCentric.ArchRules.Tests.Fixtures.Strategic.Bad";
    private const string Undeclared = "DomainCentric.ArchRules.Tests.Fixtures.Strategic.Undeclared";

    private static readonly string[] ExpectedIds =
    {
        "DCA-STR-001", "DCA-STR-002", "DCA-STR-003", "DCA-STR-004", "DCA-STR-005",
        "DCA-STR-006", "DCA-STR-007", "DCA-STR-008", "DCA-STR-009", "DCA-STR-010",
        "DCA-STR-011",
    };

    private static readonly IReadOnlyDictionary<string, string> NoNegativeFixture = new Dictionary<string, string>
    {
        ["DCA-STR-001"] = "diagnostic rule — prints the discovered contexts, never fails",
        ["DCA-STR-010"] = "documentation-only rule — verified by code review, never fails",
        ["DCA-STR-011"] = "the bad fixture declares its contexts; absence has its own fixture",
    };

    private static DcaArchitecture Arch(string ns) =>
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(ns), typeof(StrategicPatternRulesTests).Assembly);

    private static StrategicPatternRules Set(string ns) => new(DcaLayout.ForRootNamespace(ns));

    public static IEnumerable<object[]> RuleIds() => ExpectedIds.Select(id => new object[] { id });

    public static IEnumerable<object[]> NegativeRuleIds() =>
        ExpectedIds.Where(id => !NoNegativeFixture.ContainsKey(id)).Select(id => new object[] { id });

    [Fact]
    public void RuleSetHasStableShape()
    {
        var set = Set(Good);
        Assert.Equal("strategic", set.Name);
        Assert.Equal(ExpectedIds, set.Rules.Select(r => r.Id).ToArray());
        Assert.Empty(StrategicPatternRules.NotApplicable);
    }

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void GoodFixturePasses(string id)
    {
        var set = Set(Good);
        set.Rules.Single(r => r.Id == id).Check(Arch(Good));
    }

    [Fact]
    public void CodeBaseWithoutAnyDeclaredContextFails()
    {
        var layout = DcaLayout.ForRootNamespace(Undeclared);
        var arch = DcaArchitecture.Load(layout, typeof(StrategicPatternRulesTests).Assembly);

        var ex = Assert.Throws<DcaRuleViolationException>(
            () => StrategicPatternRules.AtLeastOneBoundedContextIsDeclared().Check(arch));

        Assert.Contains("[BoundedContext]", ex.Message);
        Assert.Contains("passes without having looked at anything", ex.Message);
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
