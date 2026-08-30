namespace DomainCentric.ArchRules;

/// <summary>
/// How a rule is treated when it is evaluated.
/// </summary>
/// <remarks>
/// A team adopting the catalog on an existing code base rarely satisfies all rules at once. Rather than
/// dropping a rule from the run — which hides that it was ever considered — it can be lowered to
/// <see cref="Warn"/> or <see cref="Off"/>. Both stay visible in the test report, together with the
/// reason recorded in the <see cref="DcaRuleSelection"/>.
/// </remarks>
public enum DcaSeverity
{
    /// <summary>Violations fail the build. The default for every rule.</summary>
    Error,

    /// <summary>Violations are reported but do not fail the build — the rule is being worked towards.</summary>
    Warn,

    /// <summary>The rule is not evaluated at all; it is still listed with its reason.</summary>
    Off,
}
