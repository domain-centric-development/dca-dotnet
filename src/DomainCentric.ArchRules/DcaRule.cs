using System;
using System.Collections.Generic;
using System.Linq;
using ArchUnitNET.Fluent;

namespace DomainCentric.ArchRules;

/// <summary>Factories for <see cref="IDcaRule"/> instances.</summary>
public static class DcaRule
{
    /// <summary>
    /// A rule built from a single ArchUnitNET rule derived from the architecture. The rule is evaluated
    /// and every failed result becomes one violation line. An empty selection passes.
    /// </summary>
    public static IDcaRule Of(string id, string title, string rationale, Func<DcaArchitecture, IArchRule> rule)
    {
        if (rule is null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        return new SimpleRule(id, title, rationale, arch => Evaluate(rule(arch), arch, title, rationale));
    }

    /// <summary>A rule with custom check logic (loops over contexts, reflective checks, …).</summary>
    public static IDcaRule Check(string id, string title, string rationale, Action<DcaArchitecture> check) =>
        new SimpleRule(id, title, rationale, check);

    /// <summary>
    /// Evaluates an ArchUnitNET rule against the architecture and throws
    /// <see cref="DcaRuleViolationException"/> listing every failed result. Use inside
    /// <see cref="Check"/> bodies that run several fluent rules.
    /// </summary>
    public static void Evaluate(IArchRule rule, DcaArchitecture arch, string title, string rationale)
    {
        if (rule is null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        var failed = rule.Evaluate(arch.Architecture)
            .Where(r => !r.Passed && !IsEmptySelection(r.Description))
            .Select(r => r.Description)
            .ToList();
        if (failed.Count > 0)
        {
            throw DcaRuleViolationException.Of($"{title}\nbecause {rationale}", failed);
        }
    }

    /// <summary>
    /// ArchUnitNET reports a rule whose predicate selected no object as failed ("requires positive
    /// evaluation"). DCA rules are written for code bases that may not yet contain the pattern in
    /// question, so an empty selection passes — like ArchUnit's <c>allowEmptyShould(true)</c>.
    /// </summary>
    private static bool IsEmptySelection(string description) =>
        description.Contains("requires positive evaluation", StringComparison.Ordinal);

    /// <summary>Throws a <see cref="DcaRuleViolationException"/> if <paramref name="violations"/> is not empty.</summary>
    public static void Fail(string header, IReadOnlyCollection<string> violations, string? fix = null)
    {
        if (violations.Count > 0)
        {
            throw DcaRuleViolationException.Of(header, violations, fix);
        }
    }

    private sealed class SimpleRule : IDcaRule
    {
        private readonly Action<DcaArchitecture> _check;

        internal SimpleRule(string id, string title, string rationale, Action<DcaArchitecture> check)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Rationale = rationale ?? throw new ArgumentNullException(nameof(rationale));
            _check = check ?? throw new ArgumentNullException(nameof(check));
        }

        public string Id { get; }

        public string Title { get; }

        public string Rationale { get; }

        public void Check(DcaArchitecture architecture) => _check(architecture);

        public override string ToString() => $"[{Id}] {Title}";
    }
}
