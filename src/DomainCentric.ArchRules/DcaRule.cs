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
    /// and every failed result becomes one violation line. An empty selection passes. Complete it with
    /// <see cref="Undescribed.Selecting"/> and <see cref="Selected.Checking"/>.
    /// </summary>
    public static Undescribed Of(string id, string title, string rationale, Func<DcaArchitecture, IArchRule> rule)
    {
        if (rule is null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        return new Undescribed(id, title, rationale, arch => Evaluate(rule(arch), arch, title, rationale));
    }

    /// <summary>
    /// A rule with custom check logic (loops over contexts, reflective checks, …). Complete it with
    /// <see cref="Undescribed.Selecting"/> and <see cref="Selected.Checking"/>.
    /// </summary>
    public static Undescribed Check(string id, string title, string rationale, Action<DcaArchitecture> check) =>
        new Undescribed(id, title, rationale, check ?? throw new ArgumentNullException(nameof(check)));

    /// <summary>
    /// A diagnostic entry, counted separately from enforced rules. It asserts nothing, so it does not
    /// throw: what it observes it returns, and <see cref="DcaRuleExecution"/> carries the lines in the
    /// rule's outcome. An empty list means there was nothing to observe.
    /// </summary>
    public static Undescribed Informational(string id, string title, string rationale, Func<DcaArchitecture, IReadOnlyList<string>> report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return new Undescribed(id, title, rationale, _ => { }, DcaRuleKind.Informational, report);
    }

    /// <summary>A rule whose mechanics are not yet described; not an <see cref="IDcaRule"/> until they are.</summary>
    public sealed class Undescribed
    {
        private readonly DcaRuleKind _kind;
        private readonly string _id;
        private readonly string _title;
        private readonly string _rationale;
        private readonly Action<DcaArchitecture> _check;
        private readonly Func<DcaArchitecture, IReadOnlyList<string>>? _report;
        private readonly DcaRuleMaturity _maturity;

        internal Undescribed(string id, string title, string rationale, Action<DcaArchitecture> check, DcaRuleKind kind = DcaRuleKind.Enforced, Func<DcaArchitecture, IReadOnlyList<string>>? report = null, DcaRuleMaturity maturity = DcaRuleMaturity.Released)
        {
            _kind = kind;
            _maturity = maturity;
            _report = report;
            _id = id ?? throw new ArgumentNullException(nameof(id));
            _title = title ?? throw new ArgumentNullException(nameof(title));
            _rationale = rationale ?? throw new ArgumentNullException(nameof(rationale));
            _check = check;
        }

        /// <summary>
        /// Declares the rule <see cref="DcaRuleMaturity.Experimental"/>: it keeps its identifier and its
        /// set, and runs only under a selection that opted in. Releasing it means removing this call.
        /// </summary>
        public Undescribed Experimental() =>
            new(_id, _title, _rationale, _check, _kind, _report, DcaRuleMaturity.Experimental);

        /// <summary>Names the types the rule looks at; see <see cref="IDcaRule.Selects"/>.</summary>
        public Selected Selecting(string selects) => new(this, RequireText(selects, "selects", _id));

        internal IDcaRule Complete(string selects, string checks) =>
            new SimpleRule(_id, _title, _rationale, selects, RequireText(checks, "checks", _id), _check, _kind, _report, _maturity);
    }

    /// <summary>A rule with its selection described; <see cref="Checking"/> completes it.</summary>
    public sealed class Selected
    {
        private readonly Undescribed _rule;
        private readonly string _selects;

        internal Selected(Undescribed rule, string selects)
        {
            _rule = rule;
            _selects = selects;
        }

        /// <summary>Names what the rule asserts about each selected type; see <see cref="IDcaRule.Checks"/>.</summary>
        public IDcaRule Checking(string checks) => _rule.Complete(_selects, checks);
    }

    private static string RequireText(string text, string field, string id)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException($"{id}: {field} must not be blank", field);
        }

        return text.Trim();
    }

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

        var results = rule.Evaluate(arch.Architecture).ToList();
        Observe(results);
        var failed = results
            .Where(r => !r.Passed && !IsEmptySelection(r.Description))
            .Select(r => r.Description)
            .ToList();
        if (failed.Count > 0)
        {
            throw DcaRuleViolationException.Of($"{title}\nbecause {rationale}", failed);
        }
    }

    /// <summary>
    /// What the ArchUnitNET rules a DCA rule evaluated selected, counted while
    /// <see cref="DcaRuleExecution"/> runs it: how many were evaluated and whether any of them selected an
    /// object. A rule that evaluated at least one and selected nothing passes with a note.
    /// </summary>
    internal sealed class SelectionProbe
    {
        public int Evaluated { get; set; }

        public bool AnySelected { get; set; }
    }

    private static readonly System.Threading.AsyncLocal<SelectionProbe?> Probe = new();

    /// <summary>Starts counting for one execution; <see cref="EndProbe"/> ends it.</summary>
    internal static SelectionProbe StartProbe() => Probe.Value = new SelectionProbe();

    internal static void EndProbe() => Probe.Value = null;

    private static void Observe(IReadOnlyCollection<ArchUnitNET.Fluent.EvaluationResult> results)
    {
        var probe = Probe.Value;
        if (probe is null)
        {
            return;
        }

        probe.Evaluated++;
        if (results.Any(r => !IsEmptySelection(r.Description)))
        {
            probe.AnySelected = true;
        }
    }

    /// <summary>
    /// ArchUnitNET reports a rule whose predicate selected no object as failed ("requires positive
    /// evaluation"). DCA rules are written for code bases that may not yet contain the pattern in
    /// question, so an empty selection passes — like ArchUnit's <c>allowEmptyShould(true)</c>.
    /// </summary>
    private static bool IsEmptySelection(string description) =>
        description.Contains("requires positive evaluation", StringComparison.Ordinal);

    /// <summary>
    /// Evaluates several ArchUnitNET rules that together make up one DCA rule and throws once with
    /// <em>all</em> their violations. A rule that iterates over modules — one fluent rule per module —
    /// must not stop at the first module that fails: a report naming only the first offender hides the
    /// others, and a <see cref="DcaRuleSelection"/> could not tolerate individual violations.
    /// </summary>
    public static void EvaluateAll(IEnumerable<IArchRule> rules, DcaArchitecture arch, string title, string rationale)
    {
        var failed = rules
            .SelectMany(rule =>
            {
                var results = rule.Evaluate(arch.Architecture).ToList();
                Observe(results);
                return results;
            })
            .Where(r => !r.Passed && !IsEmptySelection(r.Description))
            .Select(r => r.Description)
            .Distinct()
            .ToList();
        if (failed.Count > 0)
        {
            throw DcaRuleViolationException.Of($"{title}\nbecause {rationale}", failed);
        }
    }

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
        private readonly Func<DcaArchitecture, IReadOnlyList<string>>? _report;

        internal SimpleRule(string id, string title, string rationale, string selects, string checks, Action<DcaArchitecture> check, DcaRuleKind kind, Func<DcaArchitecture, IReadOnlyList<string>>? report, DcaRuleMaturity maturity)
        {
            Kind = kind;
            Maturity = maturity;
            _report = report;
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Rationale = rationale ?? throw new ArgumentNullException(nameof(rationale));
            Selects = selects ?? throw new ArgumentNullException(nameof(selects));
            Checks = checks ?? throw new ArgumentNullException(nameof(checks));
            _check = check ?? throw new ArgumentNullException(nameof(check));
        }

        public IReadOnlyList<string> Observe(DcaArchitecture architecture) =>
            _report is null ? Array.Empty<string>() : _report(architecture);

        public DcaRuleKind Kind { get; }

        public DcaRuleMaturity Maturity { get; }

        public string Id { get; }

        public string Title { get; }

        public string Rationale { get; }

        public string Selects { get; }

        public string Checks { get; }

        public void Check(DcaArchitecture architecture) => _check(architecture);

        public override string ToString() => $"[{Id}] {Title}";
    }
}
