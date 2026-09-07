using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Core;

public sealed class DcaRuleSelectionTests
{
    private static readonly DcaLayout Layout = DcaLayout.ForRootNamespace("Acme.Shop");

    private static IReadOnlyDictionary<string, string> Properties(params string[] lines) =>
        lines.Select(line => line.Split('=', 2))
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(), StringComparer.Ordinal);

    [Fact]
    public void AllSelectsTheWholeCatalog() =>
        Assert.Equal(DcaRules.All(Layout).Count, DcaRules.SelectFlat(Layout, DcaRuleSelection.All()).Count);

    [Fact]
    public void OnlySetsNarrowsTheRun()
    {
        var sets = DcaRules.Select(Layout, DcaRuleSelection.All().OnlySets("tactical", "cycles"));

        Assert.Equal(new[] { "tactical", "cycles" }, sets.Select(s => s.Name));
    }

    [Fact]
    public void ExcludedRuleKeepsItsReasonAndStaysInTheRun()
    {
        var selection = DcaRuleSelection.All().Excluding("DCA-NAM-005", "no DI framework");

        Assert.Equal(DcaSeverity.Off, selection.SeverityOf("DCA-NAM-005"));
        Assert.Equal("no DI framework", selection.ReasonFor("DCA-NAM-005"));
        Assert.Equal(DcaSeverity.Error, selection.SeverityOf("DCA-NAM-001"));
        Assert.True(selection.Includes("naming", "DCA-NAM-005"), "reported as skipped, not dropped");
    }

    [Fact]
    public void SeverityCanBeSetForAWholeSet()
    {
        var selection = DcaRuleSelection.All().WarningForSet("naming", "migrating");

        Assert.Equal(DcaSeverity.Warn, selection.SeverityOf("DCA-NAM-001"));
        Assert.Equal(DcaSeverity.Warn, selection.SeverityOf("DCA-NAM-011"));
        Assert.Equal(DcaSeverity.Error, selection.SeverityOf("DCA-TAC-001"));
    }

    [Fact]
    public void IgnorePatternsAccumulateAndSurviveASeverityChange()
    {
        var selection = DcaRuleSelection.All()
            .IgnoringViolationsMatching("DCA-STR-003", ".*backoffice.*")
            .IgnoringViolationsMatching("DCA-STR-003", ".*legacy.*")
            .Warning("DCA-STR-003", "phased in");

        Assert.Equal(new[] { ".*backoffice.*", ".*legacy.*" }, selection.IgnoredViolationPatterns("DCA-STR-003"));
        Assert.Equal(DcaSeverity.Warn, selection.SeverityOf("DCA-STR-003"));
    }

    [Fact]
    public void MergingLetsTheLaterSelectionWinPerRule()
    {
        var merged = DcaRuleSelection.All()
            .Warning("DCA-NAM-001", "base")
            .Excluding("DCA-NAM-003")
            .MergedWith(DcaRuleSelection.All().Excluding("DCA-NAM-001", "override"));

        Assert.Equal(DcaSeverity.Off, merged.SeverityOf("DCA-NAM-001"));
        Assert.Equal("override", merged.ReasonFor("DCA-NAM-001"));
        Assert.Equal(DcaSeverity.Off, merged.SeverityOf("DCA-NAM-003"));
    }

    [Fact]
    public void UnknownRuleIdIsRejected()
    {
        var failure = Assert.Throws<ArgumentException>(() => DcaRuleSelection.All().Excluding("DCA-NAM-999"));

        Assert.Contains("DCA-NAM-999", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownRuleSetIsRejected() =>
        Assert.Throws<ArgumentException>(() => DcaRuleSelection.All().OnlySets("tacticall"));

    [Fact]
    public void InvalidIgnorePatternIsRejected() =>
        Assert.Throws<ArgumentException>(() =>
            DcaRuleSelection.All().IgnoringViolationsMatching("DCA-NAM-001", "[unclosed"));

    [Fact]
    public void PropertiesCarrySeverityAndReason()
    {
        var selection = DcaRuleSelection.FromProperties(Properties(
            "dca.rules.off = DCA-NAM-005",
            "dca.rules.warn = DCA-TAC-009",
            "dca.rule.DCA-NAM-005.reason = no DI framework in this project",
            "dca.rule.DCA-STR-003.ignore = .*backoffice.*"));

        Assert.Equal(DcaSeverity.Off, selection.SeverityOf("DCA-NAM-005"));
        Assert.Equal(DcaSeverity.Warn, selection.SeverityOf("DCA-TAC-009"));
        Assert.Equal("no DI framework in this project", selection.ReasonFor("DCA-NAM-005"));
        Assert.Equal(new[] { ".*backoffice.*" }, selection.IgnoredViolationPatterns("DCA-STR-003"));
    }

    /// <summary>
    /// The value of an <c>.ignore</c> key is one regular expression, commas included — <c>Foo.{1,3}Bar</c>
    /// is a quantifier, not two patterns. Several expressions use indexed keys.
    /// </summary>
    [Fact]
    public void AnIgnoreExpressionIsOneRegexCommasIncluded()
    {
        var selection = DcaRuleSelection.FromProperties(Properties("dca.rule.DCA-STR-003.ignore = Foo.{1,3}Bar"));

        Assert.Equal(new[] { "Foo.{1,3}Bar" }, selection.IgnoredViolationPatterns("DCA-STR-003"));
    }

    [Fact]
    public void SeveralIgnoreExpressionsUseIndexedKeys()
    {
        var selection = DcaRuleSelection.FromProperties(Properties(
            "dca.rule.DCA-STR-003.ignore.2 = .*generated.*",
            "dca.rule.DCA-STR-003.ignore.1 = .*legacy.*",
            "dca.rule.DCA-STR-003.ignore = .*[a-z],[0-9].*"));

        Assert.Equal(new[] { ".*[a-z],[0-9].*", ".*legacy.*", ".*generated.*" }, selection.IgnoredViolationPatterns("DCA-STR-003"));
    }

    [Fact]
    public void ARuleLevelSettingWinsOverItsSet()
    {
        var selection = DcaRuleSelection.FromProperties(Properties(
            "dca.rules.warn.sets = naming",
            "dca.rules.off = DCA-NAM-005"));

        Assert.Equal(DcaSeverity.Off, selection.SeverityOf("DCA-NAM-005"));
        Assert.Equal(DcaSeverity.Warn, selection.SeverityOf("DCA-NAM-001"));
    }

    [Fact]
    public void ATypoInARuleIdFailsLoudly()
    {
        var failure = Assert.Throws<ArgumentException>(() =>
            DcaRuleSelection.FromProperties(Properties("dca.rules.off = DCA-NAM-042")));

        Assert.Contains("DCA-NAM-042", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FreezingIsRejectedWithAPointerToTheAlternative()
    {
        var failure = Assert.Throws<ArgumentException>(() =>
            DcaRuleSelection.FromProperties(Properties("dca.rules.freeze = DCA-ONI-002")));

        Assert.Contains("FreezingArchRule", failure.Message, StringComparison.Ordinal);
        Assert.Contains("dca.rules.warn", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnAbsentFileMeansTheWholeCatalog()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        try
        {
            var selection = DcaRuleSelection.FromDirectory(directory);

            Assert.Equal(DcaRules.All(Layout).Count, DcaRules.SelectFlat(Layout, selection).Count);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void AFileIsReadTheSameWayAsTheProperties()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(
                Path.Combine(directory, DcaRuleSelection.DefaultFileName),
                "# comment\ndca.rules.off = DCA-NAM-005\ndca.rule.DCA-NAM-005.reason = no DI container\n");

            var selection = DcaRuleSelection.FromDirectory(directory);

            Assert.Equal(DcaSeverity.Off, selection.SeverityOf("DCA-NAM-005"));
            Assert.Equal("no DI container", selection.ReasonFor("DCA-NAM-005"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void CatalogMetadataIsComplete()
    {
        Assert.Equal(11, DcaRules.SetNames().Count);
        Assert.Equal(DcaRules.AllIds().Count, DcaRules.SetOfRule().Count);
    }
}
