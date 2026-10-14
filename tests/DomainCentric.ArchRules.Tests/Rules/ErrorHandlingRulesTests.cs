using System.Collections.Generic;
using System.Linq;
using DomainCentric.ArchRules.Rules;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Rules;

/// <summary>Self-test of <see cref="ErrorHandlingRules"/> against the error-handling fixture tree.</summary>
public sealed class ErrorHandlingRulesTests
{
    private const string Root = "DomainCentric.ArchRules.Tests.Fixtures.Errors";
    private const string Good = Root + ".Good";
    private const string Bad = Root + ".Bad";

    /// <summary>The informational diagnostic never fails, so it has no negative fixture.</summary>
    private static readonly IReadOnlyDictionary<string, string> NoNegativeFixture = new Dictionary<string, string>
    {
        ["DCA-ERR-006"] = "informational diagnostic, never fails",
        ["DCA-ERR-007"] = "informational diagnostic, never fails - FailureFieldsAreObservedOnTheBadFixtureOnly shows it",
    };

    private static readonly string[] ExpectedIds =
    {
        "DCA-ERR-001", "DCA-ERR-002", "DCA-ERR-003", "DCA-ERR-004", "DCA-ERR-005", "DCA-ERR-006", "DCA-ERR-007",
    };

    private static DcaArchitecture Arch(string ns) =>
        DcaArchitecture.Load(
            DcaLayout.ForRootNamespace(ns).WithFrameworkTypes(FrameworkTypes.None() with
            {
                ContainerAttributeNamespaces = new[] { Root + ".Roles.Container" },
                TransportStatusAttributeTypes = new[] { Root + ".Roles.Transport.FixesAnswerAttribute" },
            }),
            typeof(ErrorHandlingRulesTests).Assembly);

    private static ErrorHandlingRules Rules(string ns) => new(Arch(ns).Layout);

    public static IEnumerable<object[]> RuleIds() => ExpectedIds.Select(id => new object[] { id });

    public static IEnumerable<object[]> NegativeRuleIds() =>
        ExpectedIds.Where(id => !NoNegativeFixture.ContainsKey(id)).Select(id => new object[] { id });

    [Fact]
    public void RuleSetHasStableShape()
    {
        var set = Rules(Good);
        Assert.Equal("errors", set.Name);
        Assert.Equal(ExpectedIds, set.Rules.Select(r => r.Id).ToArray());
        Assert.All(ErrorHandlingRules.NotApplicable.Keys, id => Assert.DoesNotContain(id, ExpectedIds));
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

    [Theory]
    [InlineData("DCA-ERR-001", "ReservationAlreadyConfirmedException")]
    [InlineData("DCA-ERR-002", "ReservationUnavailableException")]
    [InlineData("DCA-ERR-003", "ReservationNotFoundException")]
    [InlineData("DCA-ERR-004", "ReservationOverbookedException")]
    [InlineData("DCA-ERR-004", "ReservationWindowClosedException")]
    [InlineData("DCA-ERR-005", "ReservationHttpResponseException")]
    public void EachRuleNamesItsOffender(string id, string offender)
    {
        var rule = Rules(Bad).Rules.Single(r => r.Id == id);

        var message = Assert.Throws<DcaRuleViolationException>(() => rule.Check(Arch(Bad))).Message;

        Assert.Contains(offender, message);
    }

    [Fact]
    public void FailureFieldsAreObservedOnTheBadFixtureOnly()
    {
        Assert.Empty(Rules(Good).Rules.Single(r => r.Id == "DCA-ERR-007").Observe(Arch(Good)));

        var observed = Rules(Bad).Rules.Single(r => r.Id == "DCA-ERR-007").Observe(Arch(Bad));
        Assert.Contains(observed, line => line.Contains("GuestAlreadyBookedException holds _guestName as a plain string", System.StringComparison.Ordinal));
        Assert.Contains(observed, line => line.Contains("GuestAlreadyBookedException holds Reservation as the aggregate Reservation", System.StringComparison.Ordinal));
    }
}
