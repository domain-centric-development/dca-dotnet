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

    /// <summary>Convenience: run every rule against the architecture, failing on the first violation.</summary>
    public static void CheckAll(DcaArchitecture architecture)
    {
        foreach (var rule in All(architecture.Layout))
        {
            rule.Check(architecture);
        }
    }
}
