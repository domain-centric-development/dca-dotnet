using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DomainCentric.ArchRules;

/// <summary>
/// Which rules of the catalog run, and how strictly.
/// </summary>
/// <remarks>
/// <para>
/// The catalog is opinionated on purpose, but no team adopts all of it on day one — and a rule a team
/// disagrees with should be a recorded decision, not a reason to drop the library. A selection
/// expresses three things: <b>scope</b> (<see cref="OnlySets"/>, <see cref="OnlyIds"/>),
/// <b>severity</b> (<see cref="Warning(string, string?)"/>, <see cref="Excluding(string, string?)"/>)
/// and <b>exceptions</b> (<see cref="IgnoringViolationsMatching"/>).
/// </para>
/// <para>
/// Every selection runs the <see cref="DcaRuleMaturity.Released"/> rules only. An
/// <see cref="DcaRuleMaturity.Experimental"/> rule runs where the project opted in with
/// <see cref="WithExperimental"/>; scope, severity and exceptions then apply to it as to any other rule.
/// </para>
/// <para>
/// Instances are immutable; every method returns a new selection. The same configuration can live in a
/// <c>dca-archunit.properties</c> file next to the test assembly — see <see cref="FromFile"/>.
/// </para>
/// <para>
/// Unlike the Java twin there is no baseline (<c>frozen</c>) dial: ArchUnitNET has no equivalent of
/// ArchUnit's <c>FreezingArchRule</c>. Lower such rules to <see cref="DcaSeverity.Warn"/> instead.
/// </para>
/// </remarks>
public sealed class DcaRuleSelection
{
    /// <summary>Name of the properties file <see cref="FromDirectory"/> looks for.</summary>
    public const string DefaultFileName = "dca-archunit.properties";

    /// <summary>The properties key that opts in to the experimental rules: <c>dca.rules.experimental = true</c>.</summary>
    public const string ExperimentalKey = "dca.rules.experimental";

    private static readonly DcaRuleSelection Everything = new(null, null, new Dictionary<string, RuleSettings>(StringComparer.Ordinal));

    private readonly IReadOnlySet<string>? _includedSets;
    private readonly IReadOnlySet<string>? _includedIds;
    private readonly IReadOnlyDictionary<string, RuleSettings> _settings;
    private readonly IReadOnlySet<string> _retiredReferences;

    private DcaRuleSelection(
        IReadOnlySet<string>? includedSets,
        IReadOnlySet<string>? includedIds,
        IReadOnlyDictionary<string, RuleSettings> settings,
        IReadOnlySet<string>? retiredReferences = null,
        bool includesExperimental = false)
    {
        _includedSets = includedSets;
        _includedIds = includedIds;
        _settings = settings;
        _retiredReferences = retiredReferences ?? new HashSet<string>(StringComparer.Ordinal);
        IncludesExperimental = includesExperimental;
    }

    /// <summary>
    /// Whether this selection opted in to the <see cref="DcaRuleMaturity.Experimental"/> rules — through
    /// <see cref="WithExperimental"/> or <c>dca.rules.experimental = true</c>.
    /// </summary>
    public bool IncludesExperimental { get; }

    /// <summary>
    /// Retired rule identifiers this selection refers to (exclusions or severity settings of a consumer that
    /// was written against an earlier catalog). They keep loading and are reported by the test runner.
    /// </summary>
    public IReadOnlyCollection<string> RetiredReferences => _retiredReferences.OrderBy(id => id, StringComparer.Ordinal).ToList();

    /// <summary>
    /// Every released rule of every set, all at <see cref="DcaSeverity.Error"/>. Experimental rules are
    /// not part of it; <see cref="WithExperimental"/> adds them.
    /// </summary>
    public static DcaRuleSelection All() => Everything;

    /// <summary>
    /// This selection with the <see cref="DcaRuleMaturity.Experimental"/> rules added, as far as its scope
    /// reaches: <c>All().OnlySets("usecase").WithExperimental()</c> adds the experimental rules of that
    /// set only. Severity settings and tolerated violations apply to them as to released rules.
    /// </summary>
    public DcaRuleSelection WithExperimental() =>
        new(_includedSets, _includedIds, _settings, _retiredReferences, includesExperimental: true);

    /// <summary>Restricts the run to the named rule sets (<c>"tactical"</c>, <c>"hexagonal"</c>, …).</summary>
    public DcaRuleSelection OnlySets(params string[] ruleSetNames)
    {
        foreach (var name in ruleSetNames)
        {
            RequireKnownSet(name);
        }

        return new DcaRuleSelection(ToSet(ruleSetNames), _includedIds, _settings, _retiredReferences, IncludesExperimental);
    }

    /// <summary>Restricts the run to the given rule identifiers.</summary>
    public DcaRuleSelection OnlyIds(params string[] ruleIds)
    {
        foreach (var id in ruleIds)
        {
            RequireKnownId(id);
            if (DcaRules.Retired().TryGetValue(id, out var retired))
            {
                throw new ArgumentException(
                    $"{id} is retired since {retired.Since} and cannot be selected: {retired.Reason} Replacement: {retired.Replacement}", nameof(ruleIds));
            }
        }

        return new DcaRuleSelection(_includedSets, ToSet(ruleIds), _settings, _retiredReferences, IncludesExperimental);
    }

    /// <summary>Switches a rule off, recording why. The reason appears in the test report.</summary>
    public DcaRuleSelection Excluding(string ruleId, string? reason = null) =>
        WithSeverity(ruleId, DcaSeverity.Off, reason);

    /// <summary>Switches every rule of a set off.</summary>
    public DcaRuleSelection ExcludingSet(string ruleSetName, string? reason = null) =>
        WithSeverityForSet(ruleSetName, DcaSeverity.Off, reason);

    /// <summary>Reports violations of a rule without failing the build.</summary>
    public DcaRuleSelection Warning(string ruleId, string? reason = null) =>
        WithSeverity(ruleId, DcaSeverity.Warn, reason);

    /// <summary>Reports violations of every rule of a set without failing the build.</summary>
    public DcaRuleSelection WarningForSet(string ruleSetName, string? reason = null) =>
        WithSeverityForSet(ruleSetName, DcaSeverity.Warn, reason);

    /// <summary>Sets an explicit severity for one rule.</summary>
    public DcaRuleSelection WithSeverity(string ruleId, DcaSeverity severity, string? reason)
    {
        RequireKnownId(ruleId);
        var settings = new Dictionary<string, RuleSettings>(_settings, StringComparer.Ordinal);
        var current = Settings(ruleId);
        settings[ruleId] = new RuleSettings(severity, reason, current.IgnoredViolationPatterns);
        var retired = _retiredReferences;
        if (DcaRules.Retired().ContainsKey(ruleId))
        {
            retired = new HashSet<string>(_retiredReferences, StringComparer.Ordinal) { ruleId };
        }

        return new DcaRuleSelection(_includedSets, _includedIds, settings, retired, IncludesExperimental);
    }

    /// <summary>
    /// Tolerates the violations of one rule whose message matches the regular expression. Use for the
    /// documented exception the rule itself cannot express — a legacy namespace, a generated type.
    /// </summary>
    public DcaRuleSelection IgnoringViolationsMatching(string ruleId, string regex)
    {
        RequireKnownId(ruleId);
        try
        {
            _ = new Regex(regex);
        }
        catch (ArgumentException e)
        {
            throw new ArgumentException($"Not a valid regular expression for {ruleId}: {regex}", nameof(regex), e);
        }

        var settings = new Dictionary<string, RuleSettings>(_settings, StringComparer.Ordinal);
        var current = Settings(ruleId);
        var patterns = current.IgnoredViolationPatterns.Append(regex).ToList();
        settings[ruleId] = new RuleSettings(current.Severity, current.Reason, patterns);
        return new DcaRuleSelection(_includedSets, _includedIds, settings, _retiredReferences, IncludesExperimental);
    }

    /// <summary>
    /// This selection with <paramref name="other"/> applied on top — <paramref name="other"/> wins per rule.
    /// The merge includes the experimental rules when either side opted in.
    /// </summary>
    public DcaRuleSelection MergedWith(DcaRuleSelection other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var settings = new Dictionary<string, RuleSettings>(_settings, StringComparer.Ordinal);
        foreach (var entry in other._settings)
        {
            settings[entry.Key] = entry.Value;
        }

        var retired = new HashSet<string>(_retiredReferences, StringComparer.Ordinal);
        retired.UnionWith(other._retiredReferences);
        return new DcaRuleSelection(
            other._includedSets ?? _includedSets,
            other._includedIds ?? _includedIds,
            settings,
            retired,
            IncludesExperimental || other.IncludesExperimental);
    }

    /// <summary>
    /// Reads <see cref="DefaultFileName"/> from the directory, or <see cref="All"/> when there is none.
    /// </summary>
    public static DcaRuleSelection FromDirectory(string directory)
    {
        var file = Path.Combine(directory, DefaultFileName);
        return File.Exists(file) ? FromFile(file) : All();
    }

    /// <summary>
    /// Reads the selection from a properties file, using the same keys as the Java library:
    /// <code>
    /// dca.rules.sets              = tactical,hexagonal
    /// dca.rules.off               = DCA-NAM-002
    /// dca.rules.warn              = DCA-TAC-009
    /// dca.rules.warn.sets         = naming
    /// dca.rule.DCA-NAM-002.reason = no DI framework in this project
    /// dca.rule.DCA-STR-003.ignore = .*backoffice.*
    /// dca.rule.DCA-STR-003.ignore.1 = .*legacy.*
    /// dca.rule.DCA-STR-003.ignore.2 = Generated.{1,3}Client
    /// dca.rules.experimental      = true
    /// </code>
    /// The value of an <c>.ignore</c> key is <em>one</em> regular expression, commas included; a second
    /// expression for the same rule uses an indexed key (<c>.ignore.1</c>, <c>.ignore.2</c>, …, applied
    /// after the unindexed one, in numeric order). Lists of rule ids and set names are comma-separated.
    /// An unknown rule identifier or set name fails immediately — a typo must not silently leave a rule
    /// enforced. <c>dca.rules.experimental</c> takes <c>true</c> or <c>false</c> and opts in to the
    /// experimental rules (see <see cref="WithExperimental"/>); any other value fails.
    /// <c>dca.rules.freeze*</c> is not supported yet and fails with an explanatory message.
    /// </summary>
    public static DcaRuleSelection FromFile(string path) => FromProperties(ReadProperties(path));

    /// <summary>Reads the selection from already parsed key/value pairs. See <see cref="FromFile"/> for the keys.</summary>
    public static DcaRuleSelection FromProperties(IReadOnlyDictionary<string, string> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        var selection = All();

        foreach (var key in properties.Keys.Where(k => k.StartsWith("dca.rules.freeze", StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                $"'{key}' is not supported yet: the rules of this library do not expose the ArchUnitNET rule "
                + "behind them, so there is nothing for ArchUnitNET's FreezingArchRule to build a baseline "
                + "from. Lower the rule to a warning (dca.rules.warn) instead.");
        }

        if (Experimental(properties))
        {
            selection = selection.WithExperimental();
        }

        var sets = Split(Value(properties, "dca.rules.sets"));
        if (sets.Count > 0)
        {
            selection = selection.OnlySets(sets.ToArray());
        }

        var ids = Split(Value(properties, "dca.rules.ids"));
        if (ids.Count > 0)
        {
            selection = selection.OnlyIds(ids.ToArray());
        }

        foreach (var set in Split(Value(properties, "dca.rules.off.sets")))
        {
            selection = selection.ExcludingSet(set, Reason(properties, "set." + set));
        }

        foreach (var set in Split(Value(properties, "dca.rules.warn.sets")))
        {
            selection = selection.WarningForSet(set, Reason(properties, "set." + set));
        }

        foreach (var id in Split(Value(properties, "dca.rules.off")))
        {
            selection = selection.Excluding(id, Reason(properties, id));
        }

        foreach (var id in Split(Value(properties, "dca.rules.warn")))
        {
            selection = selection.Warning(id, Reason(properties, id));
        }

        foreach (var (id, expressions) in IgnoreExpressions(properties))
        {
            foreach (var regex in expressions)
            {
                selection = selection.IgnoringViolationsMatching(id, regex);
            }
        }

        return selection;
    }

    /// <summary>The value of <see cref="ExperimentalKey"/>; absent or blank means no opt-in.</summary>
    private static bool Experimental(IReadOnlyDictionary<string, string> properties)
    {
        var value = Value(properties, ExperimentalKey)?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        return bool.TryParse(value, out var experimental)
            ? experimental
            : throw new ArgumentException($"{ExperimentalKey} must be true or false, not '{value}'.");
    }

    /// <summary><c>dca.rule.&lt;id&gt;.ignore</c> and <c>dca.rule.&lt;id&gt;.ignore.&lt;n&gt;</c>.</summary>
    private static readonly Regex IgnoreKey = new(@"^dca\.rule\.(.+?)\.ignore(?:\.(\d+))?$", RegexOptions.CultureInvariant);

    /// <summary>
    /// The ignore expressions per rule id, in the order: the unindexed key first, then the indexed keys by
    /// number. Each value is one regular expression, taken as written — a comma is part of the expression
    /// (<c>Foo.{1,3}Bar</c>), never a separator.
    /// </summary>
    private static IEnumerable<(string Id, IReadOnlyList<string> Expressions)> IgnoreExpressions(IReadOnlyDictionary<string, string> properties)
    {
        var byRule = new SortedDictionary<string, SortedDictionary<int, string>>(StringComparer.Ordinal);
        foreach (var (key, value) in properties)
        {
            var match = IgnoreKey.Match(key);
            if (!match.Success || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var index = match.Groups[2].Success ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) : -1;
            if (!byRule.TryGetValue(match.Groups[1].Value, out var expressions))
            {
                expressions = new SortedDictionary<int, string>();
                byRule[match.Groups[1].Value] = expressions;
            }

            expressions[index] = value.Trim();
        }

        return byRule.Select(entry => (entry.Key, (IReadOnlyList<string>)entry.Value.Values.ToList()));
    }

    /// <summary>
    /// Whether a rule of the given set takes part in the run at all, judged by its set and identifier
    /// alone. <see cref="Includes(string, IDcaRule)"/> also weighs the rule's maturity.
    /// </summary>
    public bool Includes(string ruleSetName, string ruleId)
    {
        if (_includedSets is not null && !_includedSets.Contains(ruleSetName))
        {
            return false;
        }

        return _includedIds is null || _includedIds.Contains(ruleId);
    }

    /// <summary>
    /// Whether the rule takes part in the run: it is in scope, and it is released or this selection
    /// opted in to the experimental rules.
    /// </summary>
    public bool Includes(string ruleSetName, IDcaRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return (rule.Maturity == DcaRuleMaturity.Released || IncludesExperimental) && Includes(ruleSetName, rule.Id);
    }

    /// <summary>The severity configured for a rule, <see cref="DcaSeverity.Error"/> unless lowered.</summary>
    public DcaSeverity SeverityOf(string ruleId) => Settings(ruleId).Severity;

    /// <summary>Why a rule was lowered or switched off, if a reason was recorded.</summary>
    public string? ReasonFor(string ruleId) => Settings(ruleId).Reason;

    /// <summary>Regular expressions whose matching violations are tolerated for this rule.</summary>
    public IReadOnlyList<string> IgnoredViolationPatterns(string ruleId) => Settings(ruleId).IgnoredViolationPatterns;

    private RuleSettings Settings(string ruleId) =>
        _settings.TryGetValue(ruleId, out var settings) ? settings : RuleSettings.Enforced;

    private DcaRuleSelection WithSeverityForSet(string ruleSetName, DcaSeverity severity, string? reason)
    {
        RequireKnownSet(ruleSetName);
        var result = this;
        foreach (var rule in CatalogSets.First(s => s.Name == ruleSetName).Rules)
        {
            result = result.WithSeverity(rule.Id, severity, reason);
        }

        return result;
    }

    private static IReadOnlyList<IDcaRuleSet> CatalogSets => DcaRules.RuleSets(DcaLayout.ForRootNamespace("Catalog"));

    internal static void RequireKnownId(string ruleId)
    {
        if (!DcaRules.AllIds().Contains(ruleId) && !DcaRules.Retired().ContainsKey(ruleId))
        {
            throw new ArgumentException($"Unknown rule id: {ruleId}. See RULES.md for the catalog.", nameof(ruleId));
        }
    }

    internal static void RequireKnownSet(string ruleSetName)
    {
        var known = CatalogSets.Select(s => s.Name).ToList();
        if (!known.Contains(ruleSetName))
        {
            throw new ArgumentException(
                $"Unknown rule set: {ruleSetName}. Known sets: {string.Join(", ", known)}", nameof(ruleSetName));
        }
    }

    private static IReadOnlySet<string> ToSet(IEnumerable<string> values) =>
        new HashSet<string>(values, StringComparer.Ordinal);

    private static string? Value(IReadOnlyDictionary<string, string> properties, string key) =>
        properties.TryGetValue(key, out var value) ? value : null;

    private static string? Reason(IReadOnlyDictionary<string, string> properties, string id) =>
        Value(properties, $"dca.rule.{id}.reason");

    private static IReadOnlyList<string> Split(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();

    /// <summary>
    /// The key/value pairs of <c>dca-archunit.properties</c> in the given directory, or an empty map when
    /// the file is absent. Exposed so that settings outside the rule selection — <c>dca.framework</c>,
    /// read by <see cref="DcaLayout.ApplyConfiguredFramework"/> — come from the same file and parser.
    /// </summary>
    public static IReadOnlyDictionary<string, string> PropertiesFrom(string directory)
    {
        var file = Path.Combine(directory, DefaultFileName);
        return File.Exists(file)
            ? ReadProperties(file)
            : new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, string> ReadProperties(string path)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith('!'))
            {
                continue;
            }

            var separator = line.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            properties[key] = value;
        }

        return properties;
    }

    public override string ToString()
    {
        var parts = new List<string>
        {
            _includedSets is null ? "all sets" : "sets " + string.Join(",", _includedSets),
        };
        if (_includedIds is not null)
        {
            parts.Add("ids " + string.Join(",", _includedIds));
        }

        if (IncludesExperimental)
        {
            parts.Add("experimental");
        }

        if (_settings.Count > 0)
        {
            parts.Add(_settings.Count.ToString(CultureInfo.InvariantCulture) + " rule(s) configured");
        }

        return $"DcaRuleSelection[{string.Join(", ", parts)}]";
    }

    /// <summary>Severity, reason and tolerated violations of a single rule.</summary>
    private sealed record RuleSettings(DcaSeverity Severity, string? Reason, IReadOnlyList<string> IgnoredViolationPatterns)
    {
        internal static readonly RuleSettings Enforced = new(DcaSeverity.Error, null, Array.Empty<string>());
    }
}
