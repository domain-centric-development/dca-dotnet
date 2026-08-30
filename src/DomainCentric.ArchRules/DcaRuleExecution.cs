using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DomainCentric.ArchRules;

/// <summary>
/// Runs a single rule the way a <see cref="DcaRuleSelection"/> asks for it: at its configured severity,
/// with the tolerated violations filtered out.
/// </summary>
/// <remarks>
/// Nothing here throws on a violation — the caller decides what a <see cref="DcaRuleOutcome"/> means.
/// The xUnit base class turns it into a failed, skipped or passing test;
/// <see cref="DcaRules.CheckAll(DcaArchitecture, DcaRuleSelection)"/> into an exception.
/// </remarks>
public static class DcaRuleExecution
{
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

        var ignored = selection.IgnoredViolationPatterns(rule.Id).Select(p => new Regex(p, RegexOptions.Singleline)).ToList();
        var violations = Evaluate(rule, architecture, ignored);
        if (violations is null)
        {
            return new DcaRuleOutcome(rule.Id, DcaRuleStatus.Passed, null);
        }

        var reason = selection.ReasonFor(rule.Id);
        var message = reason is null ? violations : violations + "\n\nRecorded reason: " + reason;
        return new DcaRuleOutcome(
            rule.Id,
            severity == DcaSeverity.Warn ? DcaRuleStatus.Warned : DcaRuleStatus.Failed,
            message);
    }

    /// <summary>The violation report, or <c>null</c> when the rule holds.</summary>
    private static string? Evaluate(IDcaRule rule, DcaArchitecture architecture, IReadOnlyList<Regex> ignored)
    {
        try
        {
            rule.Check(architecture);
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

    private static bool MatchesAny(string text, IReadOnlyList<Regex> patterns) =>
        patterns.Any(pattern => pattern.IsMatch(text));
}
