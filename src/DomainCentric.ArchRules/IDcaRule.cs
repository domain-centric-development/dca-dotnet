namespace DomainCentric.ArchRules;

/// <summary>
/// One governance rule of Domain-Centric Architecture.
/// </summary>
/// <remarks>
/// A rule has a stable identifier (<c>DCA-TAC-003</c>), a human-readable title, the rationale that
/// appears in violation messages, and a check against a <see cref="DcaArchitecture"/>. Most rules wrap a
/// single ArchUnitNET rule; some iterate over the discovered bounded contexts and run several checks.
/// Identifiers are the contract shared with the Java rule library (<c>dca-archunit</c>) and the DCA
/// knowledge catalog — never renumber them.
/// </remarks>
public interface IDcaRule
{
    /// <summary>Stable identifier, e.g. <c>DCA-TAC-001</c>.</summary>
    string Id { get; }

    /// <summary>Short statement of the rule, e.g. "Aggregate Roots must implement IAggregateRoot".</summary>
    string Title { get; }

    /// <summary>Why the rule exists — used as the <c>Because(...)</c> text.</summary>
    string Rationale { get; }

    /// <summary>Runs the rule; throws <see cref="DcaRuleViolationException"/> on violation.</summary>
    void Check(DcaArchitecture architecture);
}
