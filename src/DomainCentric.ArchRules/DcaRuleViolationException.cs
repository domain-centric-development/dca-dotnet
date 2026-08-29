using System;
using System.Collections.Generic;

namespace DomainCentric.ArchRules;

/// <summary>Thrown by <see cref="IDcaRule.Check"/> when the architecture violates the rule.</summary>
public sealed class DcaRuleViolationException : Exception
{
    public DcaRuleViolationException(string message)
        : base(message)
    {
    }

    /// <summary>Builds the standard message: rule header, one line per violation, optional fix hint.</summary>
    public static DcaRuleViolationException Of(string header, IReadOnlyCollection<string> violations, string? fix = null)
    {
        var text = header + "\nViolations:\n" + string.Join("\n", violations);
        if (!string.IsNullOrEmpty(fix))
        {
            text += "\n\nFix: " + fix;
        }

        return new DcaRuleViolationException(text);
    }
}
