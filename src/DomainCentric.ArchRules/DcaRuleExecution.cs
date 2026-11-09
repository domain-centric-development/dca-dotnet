using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Freeze;

namespace DomainCentric.ArchRules;

/// <summary>
/// Runs a single rule the way a <see cref="DcaRuleSelection"/> asks for it: at its configured severity,
/// against a frozen baseline where one was requested, with the tolerated violations filtered out.
/// </summary>
/// <remarks>
/// Nothing here throws on a violation — the caller decides what a <see cref="DcaRuleOutcome"/> means.
/// The xUnit base class turns it into a failed, skipped or passing test;
/// <see cref="DcaRules.CheckAll(DcaArchitecture, DcaRuleSelection)"/> into an exception.
/// </remarks>
public static class DcaRuleExecution
{
    /// <summary>
    /// The note a passing rule carries when its selection was empty: no type of the scan matched what the
    /// rule selects, so it held for nothing. Only rules that evaluate ArchUnitNET rules can say so.
    /// </summary>
    public const string SelectedNothing =
        "selected nothing - no class of the scan matched, so the rule held for nothing; if the project"
        + " has such classes, the layout or a framework role misses them.";

    /// <summary>Evaluates one rule under the given selection.</summary>
    public static DcaRuleOutcome Execute(IDcaRule rule, DcaArchitecture architecture, DcaRuleSelection selection)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(selection);

        var severity = selection.SeverityOf(rule.Id);
        if (severity == DcaSeverity.Off)
        {
            return new DcaRuleOutcome(
                rule.Id,
                DcaRuleStatus.Skipped,
                selection.ReasonFor(rule.Id) ?? "switched off, no reason recorded");
        }

        // A diagnostic asserts nothing: it observes and hands back what it saw, so it never fails and its
        // lines travel in the outcome instead of going to the console, where no report and no pipeline
        // stage would see them.
        if (rule.Kind == DcaRuleKind.Informational)
        {
            var observed = rule.Observe(architecture);
            return new DcaRuleOutcome(
                rule.Id,
                DcaRuleStatus.Passed,
                observed.Count == 0 ? null : WithRuleId(string.Join("\n", observed), rule.Id));
        }

        var ignored = selection.IgnoredViolationPatterns(rule.Id).Select(p => new Regex(p, RegexOptions.Singleline)).ToList();
        var probe = DcaRule.StartProbe();
        string? violations;
        try
        {
            violations = Evaluate(rule, architecture, selection, ignored);
        }
        finally
        {
            DcaRule.EndProbe();
        }

        if (violations is null)
        {
            // A rule that evaluated its fluent rules and selected nothing held for nothing: it passes, but
            // says so, because a selection a layout or a framework role misses passes in silence otherwise.
            return probe.Evaluated > 0 && !probe.AnySelected
                ? new DcaRuleOutcome(rule.Id, DcaRuleStatus.Passed, WithRuleId(SelectedNothing + " Selects: " + rule.Selects, rule.Id))
                : new DcaRuleOutcome(rule.Id, DcaRuleStatus.Passed, null);
        }

        var reason = selection.ReasonFor(rule.Id);
        var report = WithRuleId(violations, rule.Id);
        var message = reason is null ? report : report + "\n\nRecorded reason: " + reason;
        return new DcaRuleOutcome(
            rule.Id,
            severity == DcaSeverity.Warn ? DcaRuleStatus.Warned : DcaRuleStatus.Failed,
            message);
    }

    /// <summary>The violation report, or <c>null</c> when the rule holds.</summary>
    private static string? Evaluate(IDcaRule rule, DcaArchitecture architecture, DcaRuleSelection selection, IReadOnlyList<Regex> ignored)
    {
        IArchRule? frozen = null;
        if (selection.IsFrozen(rule.Id))
        {
            var archRule = rule.ArchRule(architecture)
                ?? throw new ArgumentException(
                    "Rule "
                    + rule.Id
                    + " cannot be frozen: it runs several checks internally and has no single ArchUnit"
                    + " rule to build a baseline from. Lower it to WARN instead.");
            frozen = FreezingArchRule.Freeze(new Baseline(rule.Id, archRule), new JsonViolationStore(selection.FreezeStoreFile));
        }

        try
        {
            if (frozen is null)
            {
                rule.Check(architecture);
            }
            else
            {
                DcaRule.EvaluateWrapped(rule, frozen, architecture);
            }

            return null;
        }
        catch (DcaRuleViolationException violation)
        {
            if (ignored.Count == 0)
            {
                return violation.Message;
            }

            var remaining = violation.Violations.Where(v => !MatchesAny(v, ignored)).ToList();
            return remaining.Count == 0 ? null : violation.Retaining(remaining).Message;
        }
    }

    /// <summary>
    /// The rule as the baseline sees it: stored under the rule id rather than the rule's generated
    /// description, which changes with its implementation, and each violation identified by its report
    /// line rather than by the type it was found on — so a new violation on a type that already had one is
    /// still new. A selection that matched nothing is not a violation and is not recorded.
    /// </summary>
    private sealed class Baseline : IArchRule
    {
        private readonly IArchRule _rule;

        internal Baseline(string id, IArchRule rule)
        {
            Description = id;
            _rule = rule;
        }

        public string Description { get; }

        public bool HasNoViolations(Architecture architecture) => Evaluate(architecture).All(result => result.Passed);

        public IEnumerable<EvaluationResult> Evaluate(Architecture architecture) =>
            _rule.Evaluate(architecture)
                .Select(result => result.Passed
                    ? result
                    : new EvaluationResult(
                        result.EvaluatedObject,
                        new StringIdentifier(result.Description),
                        DcaRule.IsEmptySelection(result.Description),
                        result.Description,
                        this,
                        result.Architecture))
                .ToList();

        public CombinedArchRuleDefinition And() => new(this, LogicalConjunctionDefinition.And);

        public CombinedArchRuleDefinition Or() => new(this, LogicalConjunctionDefinition.Or);

        public IArchRule And(IArchRule archRule) => new CombinedArchRule(this, LogicalConjunctionDefinition.And, archRule);

        public IArchRule Or(IArchRule archRule) => new CombinedArchRule(this, LogicalConjunctionDefinition.Or, archRule);
    }

    /// <summary>Prefixes every line of a violation report with the rule id.</summary>
    /// <remarks>
    /// The id is what a reader switches off through <see cref="DcaRuleSelection"/>, what a review cites
    /// and what an agent matches a report line on — the rule title is not, because it changes with a
    /// configured suffix. A line that already carries the id is left alone.
    /// </remarks>
    private static string WithRuleId(string report, string id)
    {
        var prefix = "[" + id + "] ";
        var lines = report.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]) && !lines[i].Contains(prefix, StringComparison.Ordinal))
            {
                lines[i] = prefix + lines[i];
            }
        }

        return string.Join("\n", lines);
    }

    private static bool MatchesAny(string text, IReadOnlyList<Regex> patterns) =>
        patterns.Any(pattern => pattern.IsMatch(text));
}
