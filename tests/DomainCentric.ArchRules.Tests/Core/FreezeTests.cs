using System;
using System.IO;
using System.Linq;
using DomainCentric.ArchRules.Rules;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Core;

/// <summary>
/// The baseline dial: <c>dca.rules.freeze</c> records today's violations of a rule and fails only on new
/// ones, for exactly the rules that are one ArchUnitNET rule.
/// </summary>
public sealed class FreezeTests : IDisposable
{
    private const string Good = "DomainCentric.ArchRules.Tests.Fixtures.Naming.Good";
    private const string Bad = "DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad";

    /// <summary>DCA-NAM-001 — violated by the bad naming fixture.</summary>
    private const string RuleId = "DCA-NAM-001";

    private readonly string _store = Path.Combine(Path.GetTempPath(), "dca-freeze-" + Path.GetRandomFileName());

    public void Dispose()
    {
        if (Directory.Exists(_store))
        {
            Directory.Delete(_store, recursive: true);
        }
    }

    private static DcaArchitecture Arch(string ns) =>
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(ns), typeof(FreezeTests).Assembly);

    private DcaRuleSelection Frozen(string ruleId = RuleId) =>
        DcaRuleSelection.All().Frozen(ruleId).WithFreezeStore(_store);

    private static DcaRuleOutcome Execute(string ns, DcaRuleSelection selection, string ruleId = RuleId)
    {
        var architecture = Arch(ns);
        var rule = DcaRules.All(architecture.Layout).Single(r => r.Id == ruleId);
        return DcaRuleExecution.Execute(rule, architecture, selection);
    }

    [Fact]
    public void TheFirstRunRecordsTheViolationsAndPasses()
    {
        Assert.Equal(DcaRuleStatus.Failed, Execute(Bad, DcaRuleSelection.All()).Status);

        var outcome = Execute(Bad, Frozen());

        Assert.Equal(DcaRuleStatus.Passed, outcome.Status);
        var baseline = File.ReadAllText(Path.Combine(_store, DcaRuleSelection.FreezeStoreFileName));
        Assert.Contains(RuleId, baseline, StringComparison.Ordinal);
        Assert.Contains("does not end with", baseline, StringComparison.Ordinal);
    }

    [Fact]
    public void ARecordedViolationKeepsPassing()
    {
        Execute(Bad, Frozen());

        Assert.Equal(DcaRuleStatus.Passed, Execute(Bad, Frozen()).Status);
    }

    [Fact]
    public void AViolationTheBaselineDoesNotHoldFails()
    {
        Execute(Good, Frozen());

        var outcome = Execute(Bad, Frozen());

        Assert.Equal(DcaRuleStatus.Failed, outcome.Status);
        Assert.Contains("[" + RuleId + "]", outcome.Message, StringComparison.Ordinal);
        Assert.Contains("does not end with", outcome.Message, StringComparison.Ordinal);
        Assert.Contains("Fix: rename the class", outcome.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AFixedViolationLeavesTheBaseline()
    {
        Execute(Bad, Frozen());
        Execute(Good, Frozen());

        Assert.Equal(DcaRuleStatus.Failed, Execute(Bad, Frozen()).Status);
    }

    [Fact]
    public void AFrozenRuleStillToleratesIgnoredViolations()
    {
        Execute(Good, Frozen());

        var outcome = Execute(Bad, Frozen().IgnoringViolationsMatching(RuleId, ".*Fixtures.*"));

        Assert.Equal(DcaRuleStatus.Passed, outcome.Status);
    }

    [Fact]
    public void ARuleThatRunsSeveralChecksRefusesToBeFrozenWithJavasWording()
    {
        var multiCheck = DcaRules.All(DcaLayout.ForRootNamespace(Bad)).First(r => r.Kind == DcaRuleKind.Enforced && !r.Freezable).Id;

        var failure = Assert.Throws<ArgumentException>(() => Execute(Bad, Frozen(multiCheck), multiCheck));

        Assert.Equal(
            "Rule " + multiCheck + " cannot be frozen: it runs several checks internally and has no single ArchUnit"
            + " rule to build a baseline from. Lower it to WARN instead.",
            failure.Message);
    }

    [Fact]
    public void ASwitchedOffRuleIsNotRefusedForBeingFrozen()
    {
        var multiCheck = DcaRules.All(DcaLayout.ForRootNamespace(Bad)).First(r => r.Kind == DcaRuleKind.Enforced && !r.Freezable).Id;

        var outcome = Execute(Bad, Frozen(multiCheck).Excluding(multiCheck, "not now"), multiCheck);

        Assert.Equal(DcaRuleStatus.Skipped, outcome.Status);
    }

    /// <summary>A freezable rule hands out its ArchUnitNET rule, and only a freezable one does.</summary>
    [Fact]
    public void ExactlyTheFreezableRulesExposeAnArchRule()
    {
        var architecture = Arch(Good);
        foreach (var rule in DcaRules.All(architecture.Layout))
        {
            Assert.Equal(rule.Freezable, rule.ArchRule(architecture) is not null);
        }
    }
}
