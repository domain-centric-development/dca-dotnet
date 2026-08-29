using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;

namespace DomainCentric.ArchRules.Rules;

/// <summary>
/// Namespace cycle detection: no circular dependencies between the per-context slices of one layer
/// (domain model, application, incoming adapters, outgoing adapters).
/// </summary>
/// <remarks>Reference: Clean Architecture, Acyclic Dependencies Principle (ADP).</remarks>
public sealed class CycleRules : IDcaRuleSet
{
    /// <summary>Java rules of this set that have no .NET counterpart (id → reason). None.</summary>
    public static readonly IReadOnlyDictionary<string, string> NotApplicable = new Dictionary<string, string>();

    public CycleRules(DcaLayout layout)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Rules = new IDcaRule[]
        {
            DomainPackagesFreeOfCycles(layout),
            ApplicationLayerFreeOfCycles(layout),
            OutgoingAdaptersFreeOfCycles(layout),
            IncomingAdaptersFreeOfCycles(layout),
        };
    }

    public string Name => "cycles";

    public IReadOnlyList<IDcaRule> Rules { get; }

    /// <summary>The layout this rule set was built for.</summary>
    public DcaLayout Layout { get; }

    public static IDcaRule DomainPackagesFreeOfCycles(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-CYC-001",
            "Domain Namespaces must not have cyclic dependencies",
            "Domain model namespaces should have clear boundaries and no cycles (Acyclic Dependencies Principle)",
            arch => CheckSlices(arch, layout, $"{layout.DomainSegment}.Model", "Domain Namespaces must not have cyclic dependencies"));

    public static IDcaRule ApplicationLayerFreeOfCycles(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-CYC-002",
            "Application Layer must not have cyclic dependencies",
            "Application services should have clear boundaries and no cycles",
            arch => CheckSlices(arch, layout, layout.ApplicationSegment, "Application Layer must not have cyclic dependencies"));

    public static IDcaRule OutgoingAdaptersFreeOfCycles(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-CYC-003",
            "Outgoing Adapter Namespaces must not have cyclic dependencies",
            "Outgoing adapters should have clear boundaries and no cycles",
            arch => CheckSlices(arch, layout, $"{layout.AdapterSegment}.{layout.OutgoingSegment}", "Outgoing Adapter Namespaces must not have cyclic dependencies"));

    public static IDcaRule IncomingAdaptersFreeOfCycles(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-CYC-004",
            "Incoming Adapter Namespaces must not have cyclic dependencies",
            "Incoming adapters should have clear boundaries and no cycles",
            arch => CheckSlices(arch, layout, $"{layout.AdapterSegment}.{layout.IncomingSegment}", "Incoming Adapter Namespaces must not have cyclic dependencies"));

    /// <summary>
    /// One slice per context for the given layer: <c>Root.(*).Layer</c> and everything below it — the
    /// reading of ArchUnit's <c>Root.(*).layer..</c>. Hand-rolled because ArchUnitNET's
    /// <c>Slices().Matching(...)</c> ignores the segments after <c>(*)</c> and slices every sub-namespace of
    /// a context, which reports intra-context namespace pairs (e.g. Domain.Model ↔ Domain.Event) as cycles.
    /// </summary>
    private static void CheckSlices(DcaArchitecture arch, DcaLayout layout, string layerSegments, string title)
    {
        var pattern = new Regex(
            "^" + Regex.Escape(layout.RootNamespace) + @"\.(" + DcaLayout.Segment + @")\." + Regex.Escape(layerSegments) + @"(\..*)?$");

        var sliceOf = new Dictionary<IType, string>();
        foreach (var type in arch.Types)
        {
            if (type.Namespace is null)
            {
                continue;
            }

            var match = pattern.Match(type.Namespace.FullName);
            if (match.Success)
            {
                sliceOf[type] = match.Groups[1].Value;
            }
        }

        // slice → slice → member dependencies
        var edges = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.Ordinal);
        foreach (var (type, slice) in sliceOf.Select(e => (e.Key, e.Value)))
        {
            foreach (var dependency in type.Dependencies)
            {
                if (!sliceOf.TryGetValue(dependency.Target, out var targetSlice) || targetSlice == slice)
                {
                    continue;
                }

                if (!edges.TryGetValue(slice, out var targets))
                {
                    edges[slice] = targets = new Dictionary<string, List<string>>(StringComparer.Ordinal);
                }

                if (!targets.TryGetValue(targetSlice, out var members))
                {
                    targets[targetSlice] = members = new List<string>();
                }

                var line = type.FullName + " -> " + dependency.Target.FullName;
                if (!members.Contains(line))
                {
                    members.Add(line);
                }
            }
        }

        var violations = new List<string>();
        foreach (var cycle in FindCycles(edges))
        {
            var sb = new StringBuilder("Cycle found: ").Append(string.Join(" -> ", cycle)).Append(" -> ").Append(cycle[0]);
            for (var i = 0; i < cycle.Count; i++)
            {
                var from = cycle[i];
                var to = cycle[(i + 1) % cycle.Count];
                sb.Append('\n').Append(from).Append(" -> ").Append(to);
                foreach (var member in edges[from][to])
                {
                    sb.Append("\n\t").Append(member);
                }
            }

            violations.Add(sb.ToString());
        }

        DcaRule.Fail(title, violations, "break the cycle by moving the shared concept into one slice or behind a port");
    }

    /// <summary>Elementary cycles between slices (each reported once, starting at its smallest node).</summary>
    private static IEnumerable<List<string>> FindCycles(Dictionary<string, Dictionary<string, List<string>>> edges)
    {
        var nodes = edges.Keys.OrderBy(n => n, StringComparer.Ordinal).ToList();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var start in nodes)
        {
            var path = new List<string> { start };
            var onPath = new HashSet<string>(StringComparer.Ordinal) { start };
            foreach (var cycle in Walk(start, start, path, onPath, edges))
            {
                var key = string.Join("|", cycle);
                if (seen.Add(key))
                {
                    yield return cycle;
                }
            }
        }
    }

    private static IEnumerable<List<string>> Walk(
        string start,
        string current,
        List<string> path,
        HashSet<string> onPath,
        Dictionary<string, Dictionary<string, List<string>>> edges)
    {
        if (!edges.TryGetValue(current, out var targets))
        {
            yield break;
        }

        foreach (var next in targets.Keys.OrderBy(n => n, StringComparer.Ordinal))
        {
            if (next == start)
            {
                yield return new List<string>(path);
                continue;
            }

            // only cycles whose smallest node is the start are reported from this start
            if (string.CompareOrdinal(next, start) < 0 || onPath.Contains(next))
            {
                continue;
            }

            path.Add(next);
            onPath.Add(next);
            foreach (var cycle in Walk(start, next, path, onPath, edges))
            {
                yield return cycle;
            }

            path.RemoveAt(path.Count - 1);
            onPath.Remove(next);
        }
    }
}
