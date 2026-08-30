using System;
using System.Collections.Generic;
using System.Linq;

namespace DomainCentric.ArchRules;

/// <summary>Thrown by <see cref="IDcaRule.Check"/> when the architecture violates the rule.</summary>
public sealed class DcaRuleViolationException : Exception
{
    public DcaRuleViolationException(string message)
        : base(message)
    {
        Header = message;
        Violations = Array.Empty<string>();
    }

    private DcaRuleViolationException(string message, string header, IReadOnlyList<string> violations, string? fix)
        : base(message)
    {
        Header = header;
        Violations = violations;
        Fix = fix;
    }

    /// <summary>What the rule demands, stated once above the list.</summary>
    public string Header { get; }

    /// <summary>
    /// One entry per offending type, member or declaration. Keeping them addressable — rather than
    /// folding them into one string — is what lets a <see cref="DcaRuleSelection"/> tolerate some of
    /// them via <c>IgnoringViolationsMatching</c>.
    /// </summary>
    public IReadOnlyList<string> Violations { get; }

    /// <summary>Optional hint on how to fix the violations.</summary>
    public string? Fix { get; }

    /// <summary>Builds the standard message: rule header, one line per violation, optional fix hint.</summary>
    public static DcaRuleViolationException Of(string header, IReadOnlyCollection<string> violations, string? fix = null)
    {
        var lines = violations.ToList();
        return new DcaRuleViolationException(Format(header, lines, fix), header, lines, fix);
    }

    /// <summary>The same failure with only the violations that were not tolerated.</summary>
    public DcaRuleViolationException Retaining(IReadOnlyList<string> remaining) =>
        new(Format(Header, remaining, Fix), Header, remaining, Fix);

    private static string Format(string header, IReadOnlyList<string> violations, string? fix)
    {
        var text = violations.Count == 0 ? header : header + "\nViolations:\n" + string.Join("\n", violations);
        if (!string.IsNullOrEmpty(fix))
        {
            text += "\n\nFix: " + fix;
        }

        return text;
    }
}
