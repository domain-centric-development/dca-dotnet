using System;
using System.Linq;
using DomainCentric.ArchRules.Rules;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Core;

public sealed class DcaRuleExecutionTests
{
    private const string Good = "DomainCentric.ArchRules.Tests.Fixtures.Naming.Good";
    private const string Bad = "DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad";

    /// <summary>DCA-NAM-001 — violated by the bad naming fixture.</summary>
    private const string RuleId = "DCA-NAM-001";

    private static DcaArchitecture Arch(string ns) =>
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(ns), typeof(DcaRuleExecutionTests).Assembly);

    private static DcaRuleOutcome Execute(string ns, DcaRuleSelection selection)
    {
        var architecture = Arch(ns);
        var rule = new NamingRules(architecture.Layout).Rules.Single(r => r.Id == RuleId);
        return DcaRuleExecution.Execute(rule, architecture, selection);
    }

    [Fact]
    public void ASatisfiedRulePasses() =>
        Assert.Equal(DcaRuleStatus.Passed, Execute(Good, DcaRuleSelection.All()).Status);

    [Fact]
    public void AViolatedRuleFails() =>
        Assert.Equal(DcaRuleStatus.Failed, Execute(Bad, DcaRuleSelection.All()).Status);

    [Fact]
    public void AWarningRuleReportsWithoutFailing()
    {
        var outcome = Execute(Bad, DcaRuleSelection.All().Warning(RuleId, "being migrated"));

        Assert.Equal(DcaRuleStatus.Warned, outcome.Status);
        Assert.Contains("being migrated", outcome.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ASwitchedOffRuleIsSkippedWithItsReason()
    {
        var outcome = Execute(Bad, DcaRuleSelection.All().Excluding(RuleId, "no naming convention yet"));

        Assert.Equal(DcaRuleStatus.Skipped, outcome.Status);
        Assert.Equal("no naming convention yet", outcome.Message);
    }

    [Fact]
    public void ToleratedViolationsAreFilteredOut()
    {
        var outcome = Execute(Bad, DcaRuleSelection.All().IgnoringViolationsMatching(RuleId, ".*Fixtures.*"));

        Assert.Equal(DcaRuleStatus.Passed, outcome.Status);
    }

    [Fact]
    public void AToleratedPatternThatMatchesNothingLeavesTheRuleFailing()
    {
        var outcome = Execute(Bad, DcaRuleSelection.All().IgnoringViolationsMatching(RuleId, "nothing"));

        Assert.Equal(DcaRuleStatus.Failed, outcome.Status);
    }
}
