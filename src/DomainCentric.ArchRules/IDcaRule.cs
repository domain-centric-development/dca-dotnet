namespace DomainCentric.ArchRules;

/// <summary>
/// One governance rule of Domain-Centric Architecture.
/// </summary>
/// <remarks>
/// A rule has a stable identifier (<c>DCA-TAC-003</c>), a human-readable title, the rationale that
/// appears in violation messages, and a check against a <see cref="DcaArchitecture"/>. Most rules wrap a
/// single ArchUnitNET rule; some iterate over the discovered bounded contexts and run several checks.
/// Identifiers are the contract shared with the Java rule library (<c>dca-archunit</c>) and the DCA
/// knowledge catalog — never renumber them. Every rule also describes its mechanics in
/// <see cref="Selects"/> and <see cref="Checks"/>; the factories in <see cref="DcaRule"/> return a builder
/// that only becomes an <c>IDcaRule</c> once both are given, so the rule catalog can always say which types
/// a rule looks at and what it asserts without a reader opening the source.
/// </remarks>
public interface IDcaRule
{
    /// <summary>Stable identifier, e.g. <c>DCA-TAC-001</c>.</summary>
    string Id { get; }

    /// <summary>Short statement of the rule, e.g. "Aggregate Roots must implement IAggregateRoot".</summary>
    string Title { get; }

    /// <summary>Why the rule exists — used as the <c>Because(...)</c> text.</summary>
    string Rationale { get; }

    /// <summary>
    /// Which types the rule selects, in one to three sentences — the set the assertion runs over, named
    /// in terms of the layout ("non-interface classes under <c>&lt;module&gt;.Application</c> whose name
    /// ends with the use-case suffix"). A type outside this set is never reported.
    /// </summary>
    string Selects { get; }

    /// <summary>
    /// What the rule asserts about each selected type, in one to three sentences — including what does
    /// not satisfy it and what it does not establish.
    /// </summary>
    string Checks { get; }

    /// <summary>Runs the rule; throws <see cref="DcaRuleViolationException"/> on violation.</summary>
    void Check(DcaArchitecture architecture);
}
