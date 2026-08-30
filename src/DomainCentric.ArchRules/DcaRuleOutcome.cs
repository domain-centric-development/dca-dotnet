namespace DomainCentric.ArchRules;

/// <summary>How the evaluation of a rule ended.</summary>
public enum DcaRuleStatus
{
    /// <summary>No violations — or none left after the tolerated ones were filtered out.</summary>
    Passed,

    /// <summary>Violations of a rule at <see cref="DcaSeverity.Error"/>.</summary>
    Failed,

    /// <summary>Violations of a rule at <see cref="DcaSeverity.Warn"/> — reported, build stays green.</summary>
    Warned,

    /// <summary>The rule was switched <see cref="DcaSeverity.Off"/> and did not run.</summary>
    Skipped,
}

/// <summary>
/// What happened when a rule was evaluated under a <see cref="DcaRuleSelection"/>.
/// </summary>
/// <remarks>
/// A skipped or warned rule is not a rule that vanished: it carries the reason it was lowered, so the
/// test report still shows that the rule exists and what the team decided about it.
/// </remarks>
/// <param name="RuleId">The rule's identifier.</param>
/// <param name="Status">How the evaluation ended.</param>
/// <param name="Message">
/// The violation report for <see cref="DcaRuleStatus.Failed"/> and <see cref="DcaRuleStatus.Warned"/>,
/// the recorded reason for <see cref="DcaRuleStatus.Skipped"/>, <c>null</c> when the rule passed.
/// </param>
public sealed record DcaRuleOutcome(string RuleId, DcaRuleStatus Status, string? Message);
