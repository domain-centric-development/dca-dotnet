using System;
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
        "DCA-ERR-008", "DCA-ERR-009",
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
    [InlineData("DCA-ERR-002", "StoreRefusedReservationException")]
    [InlineData("DCA-ERR-003", "ReservationQuotaExceededException")]
    [InlineData("DCA-ERR-009", "CancelReservationUseCase.CancelAsync constructs DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Domain.Model.ReservationUnknownException")]
    [InlineData("DCA-ERR-009", "CancelReservationUseCase.UnknownFor constructs")]
    [InlineData("DCA-ERR-008", "CancelReservationResult is answered by [ICancelReservationInputPort] and none of its cases [Cancelled, NotFound]")]
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

    [Fact]
    public void ACodeHeldInAPlainStringIsListed()
    {
        var observed = Rules(Bad).Rules.Single(r => r.Id == "DCA-ERR-007").Observe(Arch(Bad));
        Assert.Contains(observed, line => line.Contains("FareMismatchException holds Left as a plain string", System.StringComparison.Ordinal));
        Assert.Contains(observed, line => line.Contains("FareMismatchException holds Right as a plain string", System.StringComparison.Ordinal));
    }

    /// <summary>DCA-ERR-006 says once that it cannot see failures of a project's own types.</summary>
    [Fact]
    public void WithoutAnyFailureMarkerTypeTheDiagnosticListsNoNamespace()
    {
        const string own = Root + ".OwnFailures";
        var observed = Rules(own).Rules.Single(r => r.Id == "DCA-ERR-006").Observe(Arch(own));

        var line = Assert.Single(observed);
        Assert.Contains("no class of the scan is assignable", line, StringComparison.Ordinal);
    }

    private const string Results = Root + ".Results";

    private static IReadOnlyList<string> ObservedOnResults(FrameworkTypes? frameworkTypes = null)
    {
        var arch = DcaArchitecture.Load(
            DcaLayout.ForRootNamespace(Results).WithFrameworkTypes(frameworkTypes ?? FrameworkTypes.None()),
            typeof(ErrorHandlingRulesTests).Assembly);
        return new ErrorHandlingRules(arch.Layout).Rules.Single(r => r.Id == "DCA-ERR-006").Observe(arch).ToList();
    }

    /// <summary>One function per case of a closed result translates the use case's refusals.</summary>
    [Fact]
    public void OneHandlerPerCaseOfAClosedResultCountsAsTranslation() =>
        Assert.DoesNotContain(ObservedOnResults(), line => line.Contains("Adapter.Incoming.Page:", StringComparison.Ordinal));

    /// <summary>A switch expression over the held case, binding no variable, translates as well.</summary>
    [Fact]
    public void ASwitchOverTheCasesOfAClosedResultCountsAsTranslation() =>
        Assert.DoesNotContain(ObservedOnResults(), line => line.Contains("Adapter.Incoming.Web:", StringComparison.Ordinal));

    /// <summary>Neither a failure type nor a case of a closed result: the namespace is still listed.</summary>
    [Fact]
    public void AnAdapterWithNeitherHandlerNorResultSwitchIsStillListed() =>
        Assert.Contains(ObservedOnResults(), line => line.Contains("Adapter.Incoming.Rest:", StringComparison.Ordinal));

    /// <summary>Holding the closed result without looking at a case is no translation.</summary>
    [Fact]
    public void ReceivingAClosedResultWithoutItsCasesIsStillListed() =>
        Assert.Contains(ObservedOnResults(), line => line.Contains("Adapter.Incoming.Feed:", StringComparison.Ordinal));

    /// <summary>The closed-result base is matched by name: without it the result is an ordinary type again.</summary>
    [Fact]
    public void WithoutAClosedResultBaseTheSwitchIsNotSeen() =>
        Assert.Contains(
            ObservedOnResults(FrameworkTypes.None() with { ClosedResultBaseTypes = Array.Empty<string>() }),
            line => line.Contains("Adapter.Incoming.Page:", StringComparison.Ordinal));

    /// <summary>Where failure types derive from the markers, DCA-ERR-006 lists namespaces as before.</summary>
    [Fact]
    public void WithFailureMarkerTypesTheDiagnosticListsNamespaces() =>
        Assert.DoesNotContain(
            Rules(Bad).Rules.Single(r => r.Id == "DCA-ERR-006").Observe(Arch(Bad)),
            line => line.Contains("no class of the scan is assignable", StringComparison.Ordinal));

    /// <summary>DCA-ERR-008: the result fixture marks its refusing cases, on a union returned two ways.</summary>
    [Fact]
    public void TheResultFixtureMarksItsRefusals()
    {
        var arch = DcaArchitecture.Load(
            DcaLayout.ForRootNamespace(Results).WithFrameworkTypes(FrameworkTypes.None()),
            typeof(ErrorHandlingRulesTests).Assembly);
        new ErrorHandlingRules(arch.Layout).Rules.Single(r => r.Id == "DCA-ERR-008").Check(arch);
    }

    /// <summary>DCA-ERR-008 selects nothing when no closed-result base is configured.</summary>
    [Fact]
    public void WithoutAClosedResultBaseNothingIsSelected()
    {
        var arch = DcaArchitecture.Load(
            DcaLayout.ForRootNamespace(Bad).WithFrameworkTypes(FrameworkTypes.None() with { ClosedResultBaseTypes = Array.Empty<string>() }),
            typeof(ErrorHandlingRulesTests).Assembly);
        new ErrorHandlingRules(arch.Layout).Rules.Single(r => r.Id == "DCA-ERR-008").Check(arch);
    }

    /// <summary>DCA-ERR-006 lists an incoming adapter that catches the abstract port failure, async body included.</summary>
    [Fact]
    public void AnAdapterCatchingThePortFailureGenericallyIsListed() =>
        Assert.Contains(
            Rules(Bad).Rules.Single(r => r.Id == "DCA-ERR-006").Observe(Arch(Bad)),
            line => line.Contains("ReservationRestController: names the abstract base PortException", StringComparison.Ordinal));

    /// <summary>DCA-ERR-006 lists no generic catch where the adapters name the failures.</summary>
    [Fact]
    public void NoGenericCatchInTheGoodFixture() =>
        Assert.DoesNotContain(
            Rules(Good).Rules.Single(r => r.Id == "DCA-ERR-006").Observe(Arch(Good)),
            line => line.Contains("names the abstract base", StringComparison.Ordinal));

    /// <summary>DCA-ERR-009: a constructor chaining to its base and the aggregate's own throw are no findings.</summary>
    [Fact]
    public void OnlyTheUseCaseConstructionIsReported()
    {
        var rule = Rules(Bad).Rules.Single(r => r.Id == "DCA-ERR-009");
        var ex = Assert.Throws<DcaRuleViolationException>(() => rule.Check(Arch(Bad)));
        Assert.DoesNotContain("Domain.Model.Reservation.", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("ReservationOverbookedException", ex.Message, StringComparison.Ordinal);
    }
}
