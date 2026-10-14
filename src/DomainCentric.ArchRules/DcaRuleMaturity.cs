namespace DomainCentric.ArchRules;

/// <summary>
/// Whether a rule is released or still on trial — independent of its <see cref="DcaRuleKind"/>.
/// </summary>
/// <remarks>
/// An experimental rule carries its final identifier and its set, so releasing it changes nothing but
/// this value. A selection runs it only when the project opted in:
/// <see cref="DcaRuleSelection.WithExperimental"/> in code, or <c>dca.rules.experimental = true</c> in
/// <c>dca-archunit.properties</c>.
/// </remarks>
public enum DcaRuleMaturity
{
    /// <summary>Part of the catalog every selection runs.</summary>
    Released,

    /// <summary>On trial: run only where the project opted in, reported apart from the released rules.</summary>
    Experimental,
}
