using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArchUnitNET.Domain;

namespace DomainCentric.ArchRules.Rules;

/// <summary>
/// Namespace cycle detection: no circular dependencies between the per-context slices of one layer
/// (domain model, application, incoming adapters, outgoing adapters), and none between the feature or
/// use-case slices inside one module's application layer.
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
            ApplicationSlicesFreeOfCycles(layout),
        };
    }

    public string Name => "cycles";

    public IReadOnlyList<IDcaRule> Rules { get; }

    /// <summary>The layout this rule set was built for.</summary>
    public DcaLayout Layout { get; }

    public static IDcaRule DomainPackagesFreeOfCycles(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-CYC-001",
            "Domain Namespaces must not have cyclic dependencies (package-based slice discovery)",
            "Domain model namespaces should have clear boundaries and no cycles (Acyclic Dependencies Principle)",
            arch => CheckSlices(arch, layout, $"{layout.DomainSegment}.Model", "Domain Namespaces must not have cyclic dependencies"))
            .Selecting(
                "One slice per module root, holding the types in <module>.Domain.Model of that module and "
                + "below. A module root is the shortest namespace prefix whose next segment is a layer "
                + "segment, so modules are found at any depth; types outside every module or outside "
                + "Domain.Model are ignored.")
            .Checking(
                "The slices form no dependency cycle - no two modules' domain models depend on each "
                + "other, directly or via further modules' domain models. Cycles between types inside one "
                + "module's domain model do not count, and dependencies into other layers do not count. "
                + "Fewer than two slices pass.");

    public static IDcaRule ApplicationLayerFreeOfCycles(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-CYC-002",
            "Application Layer must not have cyclic dependencies (package-based slice discovery)",
            "Application services should have clear boundaries and no cycles",
            arch => CheckSlices(arch, layout, layout.ApplicationSegment, "Application Layer must not have cyclic dependencies"))
            .Selecting(
                "One slice per module root, holding the types in <module>.Application of that module and "
                + "below (Application.Shared included); types outside every module or outside the "
                + "application layer are ignored.")
            .Checking(
                "The slices form no dependency cycle between modules' application layers. Cycles between "
                + "use cases or features inside one module do not count here (see DCA-CYC-005), nor do "
                + "dependencies into domain or adapter types.");

    public static IDcaRule OutgoingAdaptersFreeOfCycles(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-CYC-003",
            "Outgoing Adapter Namespaces must not have cyclic dependencies",
            "Outgoing adapters should have clear boundaries and no cycles",
            arch => CheckSlices(arch, layout, $"{layout.AdapterSegment}.{layout.OutgoingSegment}", "Outgoing Adapter Namespaces must not have cyclic dependencies"))
            .Selecting(
                "One slice per module root, holding the types in <module>.Adapter.Outgoing of that module "
                + "and below; everything else is ignored.")
            .Checking(
                "The slices form no dependency cycle between modules' outgoing adapters. Cycles inside "
                + "one module's outgoing adapters and dependencies into other layers do not count.");

    public static IDcaRule IncomingAdaptersFreeOfCycles(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-CYC-004",
            "Incoming Adapter Namespaces must not have cyclic dependencies",
            "Incoming adapters should have clear boundaries and no cycles",
            arch => CheckSlices(arch, layout, $"{layout.AdapterSegment}.{layout.IncomingSegment}", "Incoming Adapter Namespaces must not have cyclic dependencies"))
            .Selecting(
                "One slice per module root, holding the types in <module>.Adapter.Incoming of that module "
                + "and below; everything else is ignored.")
            .Checking(
                "The slices form no dependency cycle between modules' incoming adapters. Cycles inside "
                + "one module's incoming adapters and dependencies into other layers do not count.");

    /// <summary>
    /// One slice per module for the given layer: <c>module.Layer</c> and everything below it, where the module
    /// is the structural <see cref="DcaArchitecture.ModuleRootOf(string)"/> — so two contexts grouped below an
    /// intermediate namespace are two slices, at any depth. Hand-rolled because ArchUnitNET's
    /// <c>Slices().Matching(...)</c> ignores the segments after <c>(*)</c> and slices every sub-namespace of
    /// a context, which reports intra-context namespace pairs (e.g. Domain.Model ↔ Domain.Event) as cycles.
    /// </summary>
    private static void CheckSlices(DcaArchitecture arch, DcaLayout layout, string layerSegments, string title) =>
        CheckSlices(
            arch,
            ns =>
            {
                var module = arch.ModuleRootOf(ns);
                return module is not null && DcaLayout.IsBelow(ns, module + "." + layerSegments) ? module : null;
            },
            title);

    /// <summary>
    /// The immediate child namespaces of a module's application namespace, <c>Shared</c> excepted, must be free
    /// of cycles. In a grouped layout those children are features, in a flat layout they are the use cases
    /// themselves.
    /// </summary>
    public static IDcaRule ApplicationSlicesFreeOfCycles(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-CYC-005",
            "Feature and use case namespaces within a module's application layer must not have cyclic dependencies",
            "The namespaces directly below a module's application namespace are its features"
                + " (Application.<Feature>.<UseCase>) or, in a flat layout, its use cases (Application.<UseCase>). A"
                + " feature is an optional, domain-named group of related use cases; it may depend on another feature"
                + " in one direction, but a cycle between two of them means the grouping does not carry its weight -"
                + " the shared concept belongs in Application.Shared, in the domain, or in one of the two."
                + " Application.Shared is the context-wide port namespace and is not a slice. The rule does not infer"
                + " bounded contexts or aggregate ownership from the namespaces it slices",
            arch => CheckSlices(
                arch,
                ns => ApplicationChildSlice(arch, layout, ns),
                "Feature and use case namespaces within a module's application layer must not have cyclic dependencies"))
            .Selecting(
                "One slice per operation-root namespace selected by marker or suffix, configured containers stripped; supporting sub-namespaces join their nearest operation root. Classes directly in a feature namespace form its feature slice. Shared and direct application types are ignored.")
            .Checking(
                "The slices form no dependency cycle: two features or two use cases that depend on each "
                + "other, directly or through further slices, are reported. Dependencies on "
                + "Application.Shared, the domain or an adapter do not count. Slices of all modules are "
                + "checked together, so a cycle through another module's use-case namespace is reported "
                + "here as well.");

    /// <summary>
    /// The slice of a namespace for <c>DCA-CYC-005</c>: <c>module.Application.&lt;child&gt;</c>, where the module is
    /// the structural <see cref="DcaArchitecture.ModuleRootOf(string)"/> - so the slicing holds at any depth and
    /// never assumes a module is a direct child of the root namespace. Types directly in the application namespace
    /// and everything below <c>Application.Shared</c> belong to no slice.
    /// </summary>
    private static string? ApplicationChildSlice(DcaArchitecture arch, DcaLayout layout, string ns)
    {
        var module = arch.ModuleRootOf(ns);
        if (module is null)
        {
            return null;
        }

        var application = module + "." + layout.ApplicationSegment;
        if (!ns.StartsWith(application + ".", StringComparison.Ordinal))
        {
            return null;
        }

        var segments = ns.Substring(application.Length + 1).Split('.');
        var index = 0;
        while (index < segments.Length && layout.OperationContainers.Contains(segments[index])) index++;
        if (index == segments.Length || segments[index] == "Shared") return null;
        var operationRoot = arch.Types.Where(t => OperationPolicy.Operation(t, arch)).Select(t => t.Namespace.FullName)
            .Where(p => p.StartsWith(application + ".", StringComparison.Ordinal) && DcaLayout.IsBelow(ns, p))
            .OrderByDescending(p => p.Length).FirstOrDefault();
        var physicalFeature = application + "." + string.Join(".", segments.Take(index + 1));
        if (operationRoot is null)
        {
            if (!arch.Types.Any(t => OperationPolicy.Operation(t, arch) && t.Namespace.FullName.StartsWith(physicalFeature + ".", StringComparison.Ordinal))) return null;
            operationRoot = physicalFeature;
        }
        var logical = string.Join(".", operationRoot[(application.Length + 1)..].Split('.').Where(segment => !layout.OperationContainers.Contains(segment)));
        return application + "." + logical;
    }

    /// <summary>
    /// Generic slice-cycle check: <paramref name="sliceOfNamespace"/> assigns each type's namespace to a slice (or
    /// to none), dependencies between different slices form the graph, and every elementary cycle is one violation.
    /// </summary>
    private static void CheckSlices(DcaArchitecture arch, Func<string, string?> sliceOfNamespace, string title)
    {
        var sliceOf = new Dictionary<IType, string>();
        foreach (var type in arch.Types)
        {
            if (type.Namespace is null)
            {
                continue;
            }

            var slice = sliceOfNamespace(type.Namespace.FullName);
            if (slice is not null)
            {
                sliceOf[type] = slice;
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
