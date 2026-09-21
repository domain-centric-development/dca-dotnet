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

    /// <summary>
    /// Every line of a report carries the rule id, so that a reader can switch the rule off, a review
    /// can cite it and an agent can map the line back to its catalog entry. The title cannot serve that
    /// purpose because it changes with a configured suffix.
    /// </summary>
    [Fact]
    public void EveryReportLineCarriesTheRuleId()
    {
        var outcome = Execute(Bad, DcaRuleSelection.All());

        Assert.Equal(DcaRuleStatus.Failed, outcome.Status);
        foreach (var line in outcome.Message!.Split('\n'))
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                Assert.Contains("[" + RuleId + "]", line, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>The id appears once per line, never twice.</summary>
    [Fact]
    public void TheRuleIdIsNotRepeatedOnALineThatAlreadyCarriesIt()
    {
        var outcome = Execute(Bad, DcaRuleSelection.All());

        foreach (var line in outcome.Message!.Split('\n'))
        {
            var marker = "[" + RuleId + "]";
            Assert.Equal(line.IndexOf(marker, StringComparison.Ordinal), line.LastIndexOf(marker, StringComparison.Ordinal));
        }
    }
}
