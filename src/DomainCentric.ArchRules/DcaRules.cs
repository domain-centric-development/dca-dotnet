using System;
using System.Collections.Generic;
using System.Linq;
using DomainCentric.ArchRules.Rules;

namespace DomainCentric.ArchRules;

/// <summary>
/// Entry point to the DCA rule catalog.
/// </summary>
/// <example>
/// <code>
/// var arch = DcaArchitecture.Load(DcaLayout.ForRootNamespace("Acme.Shop"), typeof(Program).Assembly);
/// DcaRules.CheckAll(arch, DcaRuleSelection.All());
/// </code>
/// For xUnit, derive from <c>DcaArchitectureTest</c> (package DomainCentric.ArchRules.Xunit) instead —
/// it turns every rule into one theory case.
/// </example>
public static class DcaRules
{
    /// <summary>A retired identity is never reused.</summary>
    public sealed record RetiredRule(string Reason, string Replacement, string Since);

    /// <summary>Stable registry for compatibility, migration and catalog history.</summary>
    public static IReadOnlyDictionary<string, RetiredRule> Retired() => RetiredRegistry;
    private static readonly IReadOnlyDictionary<string, RetiredRule> RetiredRegistry =
        new System.Collections.ObjectModel.ReadOnlyDictionary<string, RetiredRule>(new Dictionary<string, RetiredRule>
        {
            ["DCA-MAP-003"] = new("Renderer disambiguates normalized external-system identifiers", "ContextMapRenderer", "0.4.0"),
            ["DCA-ADV-003"] = new("Immutable event shape is checked by DCA-ADV-001", "DCA-ADV-001", "0.4.0"),
            ["DCA-TAC-022"] = new("Selected on the sample's own pattern name (Enriched*), which a general rule may not carry; value models are covered by DCA-TAC-008..012 and naming one enriched remains guide and catalog guidance", "DCA-TAC-008..012", "0.4.0"),
            ["DCA-ADV-009"] = new("Where a domain service sits inside the domain layer is the team's decision; DCA-ADV-010 keeps the layer itself", "DCA-ADV-010", "0.5.0"),
            ["DCA-STR-009"] = new("A separate segment for the anti-corruption layer is a filing decision, and the selection named three suffixes a project cannot change; the pattern stays guide and catalog guidance", "none", "0.5.0"),
            ["DCA-HEX-015"] = new("Keeping no state for a later request in one instance is an operational requirement - several instances without sticky sessions - that a project chooses, not architecture every application shares; the pattern stays guide and catalog guidance for applications that run that way", "none", "0.9.0")
        });

    /// <summary>All rule sets, in catalog order.</summary>
    public static IReadOnlyList<IDcaRuleSet> RuleSets(DcaLayout layout) =>
        new IDcaRuleSet[]
        {
            new LayeredRules(layout),
            new OnionRules(layout),
            new HexagonalRules(layout),
            new TacticalPatternRules(layout),
            new StrategicPatternRules(layout),
            new ContextMapRules(layout),
            new AdvancedPatternRules(layout),
            new UseCaseRules(layout),
            new NamingRules(layout),
            new CycleRules(layout),
            new DotnetRules(layout),
            new ErrorHandlingRules(layout),
        };

    /// <summary>
    /// Every rule of every set — the whole catalog, experimental rules included. To run rules, go through
    /// <see cref="Select"/> or <see cref="CheckAll(DcaArchitecture, DcaRuleSelection)"/>, which leave the
    /// experimental rules out unless the selection opted in.
    /// </summary>
    public static IReadOnlyList<IDcaRule> All(DcaLayout layout) =>
        RuleSets(layout).SelectMany(s => s.Rules).ToList();

    /// <summary>
    /// Every released rule except the given identifiers. An identifier that is neither in the catalog nor
    /// retired is rejected: it would exclude nothing and leave the caller believing it had.
    /// </summary>
    public static IReadOnlyList<IDcaRule> AllExcept(DcaLayout layout, IEnumerable<string> excludedIds)
    {
        var excluded = new HashSet<string>(excludedIds, StringComparer.Ordinal);
        foreach (var id in excluded)
        {
            DcaRuleSelection.RequireKnownId(id);
        }

        return All(layout).Where(r => r.Maturity == DcaRuleMaturity.Released && !excluded.Contains(r.Id)).ToList();
    }

    /// <summary>
    /// The released rules of the named sets only (<c>"tactical"</c>, <c>"hexagonal"</c>, …). An unknown
    /// set name is rejected rather than silently selecting no rule.
    /// </summary>
    public static IReadOnlyList<IDcaRule> Only(DcaLayout layout, params string[] ruleSetNames)
    {
        var names = new HashSet<string>(ruleSetNames, StringComparer.Ordinal);
        foreach (var name in names)
        {
            DcaRuleSelection.RequireKnownSet(name);
        }

        return RuleSets(layout)
            .Where(s => names.Contains(s.Name))
            .SelectMany(s => s.Rules)
            .Where(r => r.Maturity == DcaRuleMaturity.Released)
            .ToList();
    }

    /// <summary>Identifiers of every rule in the catalog — layout-independent, in catalog order.</summary>
    public static IReadOnlyList<string> AllIds() => All(DcaLayout.ForRootNamespace("Catalog")).Select(r => r.Id).ToList();

    /// <summary>The rule with the given identifier, built for the layout.</summary>
    public static IDcaRule ById(DcaLayout layout, string id) =>
        All(layout).FirstOrDefault(r => r.Id == id) ?? throw new KeyNotFoundException($"no DCA rule with id {id}");

    /// <summary>
    /// The sets and rules the selection asks for, in catalog order, empty sets removed. An experimental
    /// rule is among them only when the selection opted in (<see cref="DcaRuleSelection.WithExperimental"/>).
    /// </summary>
    public static IReadOnlyList<IDcaRuleSet> Select(DcaLayout layout, DcaRuleSelection selection)
    {
        var selected = new List<IDcaRuleSet>();
        foreach (var set in RuleSets(layout))
        {
            var rules = set.Rules.Where(r => selection.Includes(set.Name, r)).ToList();
            if (rules.Count > 0)
            {
                selected.Add(new SelectedRuleSet(set.Name, rules));
            }
        }

        return selected;
    }

    /// <summary>The rules the selection asks for, flattened.</summary>
    public static IReadOnlyList<IDcaRule> SelectFlat(DcaLayout layout, DcaRuleSelection selection) =>
        Select(layout, selection).SelectMany(s => s.Rules).ToList();

    /// <summary>The names of the rule sets, in catalog order.</summary>
    public static IReadOnlyList<string> SetNames() =>
        RuleSets(DcaLayout.ForRootNamespace("Catalog")).Select(s => s.Name).ToList();

    /// <summary>The set a rule belongs to, keyed by rule id.</summary>
    public static IReadOnlyDictionary<string, string> SetOfRule()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var set in RuleSets(DcaLayout.ForRootNamespace("Catalog")))
        {
            foreach (var rule in set.Rules)
            {
                map[rule.Id] = set.Name;
            }
        }

        return map;
    }

    /// <summary>Convenience: run every released rule against the architecture, failing on the first violation.</summary>
    public static void CheckAll(DcaArchitecture architecture) => CheckAll(architecture, DcaRuleSelection.All());

    /// <summary>
    /// Runs the selected rules, failing on the first rule at <see cref="DcaSeverity.Error"/> that is
    /// violated. Warnings go to the error stream, skipped rules are silent.
    /// </summary>
    public static void CheckAll(DcaArchitecture architecture, DcaRuleSelection selection)
    {
        foreach (var rule in SelectFlat(architecture.Layout, selection))
        {
            var outcome = DcaRuleExecution.Execute(rule, architecture, selection);
            switch (outcome.Status)
            {
                case DcaRuleStatus.Failed:
                    throw new DcaRuleViolationException($"[{rule.Id}] {outcome.Message}");
                case DcaRuleStatus.Warned:
                    Console.Error.WriteLine($"[{rule.Id}] WARNING: {outcome.Message}");
                    break;
                default:
                    break;
            }
        }
    }

    private sealed record SelectedRuleSet(string Name, IReadOnlyList<IDcaRule> Rules) : IDcaRuleSet;
}
