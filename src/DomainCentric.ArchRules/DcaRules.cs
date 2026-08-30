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
/// foreach (var rule in DcaRules.All(arch.Layout))
/// {
///     rule.Check(arch);
/// }
/// </code>
/// For xUnit, derive from <c>DcaArchitectureTest</c> (package DomainCentric.ArchRules.Xunit) instead —
/// it turns every rule into one theory case.
/// </example>
public static class DcaRules
{
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
        };

    /// <summary>Every rule of every set.</summary>
    public static IReadOnlyList<IDcaRule> All(DcaLayout layout) =>
        RuleSets(layout).SelectMany(s => s.Rules).ToList();

    /// <summary>Every rule except the given identifiers.</summary>
    public static IReadOnlyList<IDcaRule> AllExcept(DcaLayout layout, IEnumerable<string> excludedIds)
    {
        var excluded = new HashSet<string>(excludedIds, StringComparer.Ordinal);
        return All(layout).Where(r => !excluded.Contains(r.Id)).ToList();
    }

    /// <summary>The rules of the named sets only (<c>"tactical"</c>, <c>"hexagonal"</c>, …).</summary>
    public static IReadOnlyList<IDcaRule> Only(DcaLayout layout, params string[] ruleSetNames)
    {
        var names = new HashSet<string>(ruleSetNames, StringComparer.Ordinal);
        return RuleSets(layout).Where(s => names.Contains(s.Name)).SelectMany(s => s.Rules).ToList();
    }

    /// <summary>Identifiers of every rule in the catalog — layout-independent, in catalog order.</summary>
    public static IReadOnlyList<string> AllIds() => All(DcaLayout.ForRootNamespace("Catalog")).Select(r => r.Id).ToList();

    /// <summary>The rule with the given identifier, built for the layout.</summary>
    public static IDcaRule ById(DcaLayout layout, string id) =>
        All(layout).FirstOrDefault(r => r.Id == id) ?? throw new KeyNotFoundException($"no DCA rule with id {id}");

    /// <summary>The sets and rules the selection asks for, in catalog order, empty sets removed.</summary>
    public static IReadOnlyList<IDcaRuleSet> Select(DcaLayout layout, DcaRuleSelection selection)
    {
        var selected = new List<IDcaRuleSet>();
        foreach (var set in RuleSets(layout))
        {
            var rules = set.Rules.Where(r => selection.Includes(set.Name, r.Id)).ToList();
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

    /// <summary>Convenience: run every rule against the architecture, failing on the first violation.</summary>
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
