using System;
using System.Collections.Generic;
using System.Linq;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace DomainCentric.ArchRules.Rules;

/// <summary>
/// DDD strategic pattern rules: shared-kernel independence, bounded-context isolation, Open Host
/// Services, Integration Events and Anti-Corruption Layers. Bounded contexts and the shared kernel
/// are discovered via <c>[BoundedContext]</c> / <c>[SharedKernel]</c> on the context marker class.
/// </summary>
public sealed class StrategicPatternRules : IDcaRuleSet
{
    /// <summary>Java rules of this set that have no .NET counterpart (id → reason). None.</summary>
    public static readonly IReadOnlyDictionary<string, string> NotApplicable = new Dictionary<string, string>();

    public StrategicPatternRules(DcaLayout layout)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Rules = new IDcaRule[]
        {
            DisplayDiscoveredBoundedContexts(),
            SharedKernelMustNotDependOnBoundedContexts(),
            ApplicationLayerIsolation(),
            DomainLayerIsolation(),
            OpenHostServicesResideInApiOrOpenHostNamespaces(),
            OutgoingAdaptersOnlyUseOpenHostServices(),
            IntegrationEventsResideInEventsNamespaces(),
            IntegrationEventsAreRecords(),
            AntiCorruptionLayerComponentsResideInAclNamespaces(),
            EventListenersUseAntiCorruptionLayer(),
        };
    }

    public string Name => "strategic";

    public IReadOnlyList<IDcaRule> Rules { get; }

    /// <summary>The layout this rule set was built for.</summary>
    public DcaLayout Layout { get; }

    /// <summary>DCA-STR-001.</summary>
    public static IDcaRule DisplayDiscoveredBoundedContexts() =>
        DcaRule.Check(
            "DCA-STR-001",
            "Diagnostic: Display discovered bounded contexts",
            "Making the discovered contexts visible shows which namespaces the strategic rules govern",
            arch =>
            {
                Console.WriteLine("=== Discovered Bounded Contexts ===");
                foreach (var e in arch.BoundedContexts)
                {
                    Console.WriteLine("  " + e.Value.Name + ": " + e.Key);
                    if (e.Value.Description.Length > 0)
                    {
                        Console.WriteLine("    Description: " + e.Value.Description);
                    }
                }
                Console.WriteLine("=== Shared Kernel ===");
                Console.WriteLine("  Namespace: " + (arch.SharedKernelNamespace ?? "<none>"));
                Console.WriteLine("==================================");
            });

    /// <summary>DCA-STR-002.</summary>
    public static IDcaRule SharedKernelMustNotDependOnBoundedContexts()
    {
        const string title = "Shared Kernel must not have dependencies on any bounded context";
        const string rationale = "Shared Kernel must be context-independent — it is shared by all contexts and owned by none";
        return DcaRule.Check(
            "DCA-STR-002",
            title,
            rationale,
            arch =>
            {
                var sharedKernel = arch.SharedKernelNamespace;
                if (sharedKernel is null)
                {
                    return;
                }
                foreach (var ctx in arch.BoundedContexts)
                {
                    DcaRule.Evaluate(
                        Types().That().ResideInNamespaceMatching(DcaLayout.Below(sharedKernel))
                            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(DcaLayout.Below(ctx.Key)),
                        arch,
                        title,
                        "Shared Kernel must not depend on bounded context '" + ctx.Value.Name + "' (" + ctx.Key
                            + ") - Shared Kernel must be context-independent");
                }
            });
    }

    /// <summary>DCA-STR-003.</summary>
    public IDcaRule ApplicationLayerIsolation()
    {
        const string title = "Bounded contexts must not directly access each other in application layer (except allowed dependencies)";
        return DcaRule.Check(
            "DCA-STR-003",
            title,
            "Application layers talk to other contexts through output ports and adapters, never directly",
            arch =>
            {
                foreach (var source in arch.BoundedContexts)
                {
                    var forbidden = arch.BoundedContextPatternsExcluding(source.Key);
                    if (forbidden.Length == 0)
                    {
                        continue;
                    }
                    // Dependencies, not accesses: a field, parameter or record component of a foreign
                    // type is a dependency even without a method call.
                    DcaRule.Evaluate(
                        Types().That().ResideInNamespaceMatching(Layout.ApplicationPatternOf(source.Key))
                            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(AnyOf(forbidden)),
                        arch,
                        title,
                        "Application layer of bounded context '" + source.Value.Name
                            + "' must not access other contexts directly - define output ports and use adapters instead");
                }
            });
    }

    /// <summary>DCA-STR-004.</summary>
    public IDcaRule DomainLayerIsolation()
    {
        const string title = "Bounded contexts must not access each other in the domain layer";
        return DcaRule.Check(
            "DCA-STR-004",
            title,
            "A domain layer talks to its own context and the shared kernel, nothing else — not even another context's Api/",
            arch =>
            {
                foreach (var source in arch.BoundedContexts)
                {
                    var forbidden = arch.BoundedContextPatternsExcluding(source.Key);
                    if (forbidden.Length == 0)
                    {
                        continue;
                    }
                    DcaRule.Evaluate(
                        Types().That().ResideInNamespaceMatching(Layout.DomainPatternOf(source.Key))
                            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(AnyOf(forbidden)),
                        arch,
                        title,
                        "The domain layer of bounded context '" + source.Value.Name
                            + "' must depend on nothing outside its own context and the shared kernel");
                }
            });
    }

    /// <summary>DCA-STR-005.</summary>
    public IDcaRule OpenHostServicesResideInApiOrOpenHostNamespaces() =>
        DcaRule.Of(
            "DCA-STR-005",
            "Open Host Services must reside in Api or Adapter.Incoming.OpenHost namespaces",
            "Open Host Services expose context capabilities via Api/ namespaces (published named interface) or"
                + " Adapter.Incoming.OpenHost/ namespaces",
            arch =>
                Types().That().HaveAnyAttributes(typeof(OpenHostServiceAttribute))
                    .Should().ResideInNamespaceMatching(AnyOf(AnySegment("Api"), OpenHostAdapterPattern())));

    private string OpenHostAdapterPattern() =>
        AnySegmentPath(Layout.AdapterSegment + "." + Layout.IncomingSegment + ".OpenHost");

    /// <summary>DCA-STR-006.</summary>
    public IDcaRule OutgoingAdaptersOnlyUseOpenHostServices()
    {
        const string title = "Outgoing adapters accessing other contexts must only use OpenHostService classes (except allowed ACL patterns)";
        return DcaRule.Check(
            "DCA-STR-006",
            title,
            "Cross-context communication goes through the published Api/ and Events/ namespaces, never through"
                + " another context's domain or application layer",
            arch =>
            {
                var contexts = arch.BoundedContexts;
                foreach (var source in contexts)
                {
                    foreach (var target in contexts)
                    {
                        if (target.Key == source.Key)
                        {
                            continue;
                        }
                        DcaRule.Evaluate(
                            Types().That().ResideInNamespaceMatching(Layout.OutgoingAdapterPatternOf(source.Key))
                                .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(Layout.DomainPatternOf(target.Key)),
                            arch,
                            title,
                            "Outgoing adapters in '" + source.Value.Name + "' must not access domain layer of '"
                                + target.Value.Name + "' - use Api/ or Events/ namespaces instead");
                        DcaRule.Evaluate(
                            Types().That().ResideInNamespaceMatching(Layout.OutgoingAdapterPatternOf(source.Key))
                                .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(Layout.ApplicationPatternOf(target.Key)),
                            arch,
                            title,
                            "Outgoing adapters in '" + source.Value.Name + "' must not access application layer of '"
                                + target.Value.Name + "' - use Api/ or Events/ namespaces instead");
                    }
                }
            });
    }

    /// <summary>DCA-STR-007.</summary>
    public IDcaRule IntegrationEventsResideInEventsNamespaces() =>
        DcaRule.Of(
            "DCA-STR-007",
            "Integration Events must be in Events or adapter outgoing event namespaces",
            "Integration Events must be in Events/ namespaces (published named interface) or Adapter.Outgoing.Event/ namespaces",
            arch =>
                Types().That().ImplementInterface(typeof(IIntegrationEvent))
                    .Should().ResideInNamespaceMatching(AnyOf(AnySegment("Events"), OutgoingEventAdapterPattern())));

    private string OutgoingEventAdapterPattern() =>
        AnySegmentPath(Layout.AdapterSegment + "." + Layout.OutgoingSegment + ".Event");

    /// <summary>DCA-STR-008.</summary>
    public static IDcaRule IntegrationEventsAreRecords() =>
        DcaRule.Check(
            "DCA-STR-008",
            "Integration Events should be immutable records",
            "Integration Events must be immutable to ensure event integrity across contexts (Event Sourcing best practice)",
            arch =>
            {
                // record structs are Structs in ArchUnitNET and therefore never reach this loop.
                var violations = arch.Classes
                    .Where(c => c.ImplementedInterfaces.Any(i => i.FullName == typeof(IIntegrationEvent).FullName))
                    .Where(c => !c.IsRecord.GetValueOrDefault())
                    .Select(c => "Integration event " + c.FullName + " is not a record")
                    .ToList();
                DcaRule.Fail(
                    "Integration Events should be immutable records",
                    violations,
                    "declare the event as a sealed record (or record struct)");
            });

    /// <summary>DCA-STR-009.</summary>
    public static IDcaRule AntiCorruptionLayerComponentsResideInAclNamespaces() =>
        DcaRule.Of(
            "DCA-STR-009",
            "Anti-Corruption Layer components must be in Acl namespaces",
            "Anti-Corruption Layer components must be in 'Acl' namespaces for clear architectural intent (DDD Strategic Pattern)",
            arch =>
                Types().That().HaveNameMatching("(EventTranslator|ACL|AntiCorruptionLayer)$")
                    .Should().ResideInNamespaceMatching(AnySegment("Acl")));

    /// <summary>DCA-STR-010 — documentation only, never fails.</summary>
    public static IDcaRule EventListenersUseAntiCorruptionLayer() =>
        DcaRule.Check(
            "DCA-STR-010",
            "Event Listeners consuming integration events should use Anti-Corruption Layer",
            "Consumed integration events are translated into the consuming context's own language before they reach"
                + " its domain — verified by code review, not statically",
            arch => { });

    // ---------------------------------------------------------------------------------------------
    // Pattern helpers (regular expressions over full namespace names)
    // ---------------------------------------------------------------------------------------------

    /// <summary>Alternation of complete namespace patterns (each already anchored).</summary>
    private static string AnyOf(params string[] patterns) =>
        string.Join("|", patterns.Select(p => "(?:" + p + ")"));

    /// <summary>ArchUnit <c>..Segment..</c>: the segment appears anywhere in the namespace.</summary>
    private static string AnySegment(string segment) => AnySegmentPath(segment);

    /// <summary>ArchUnit <c>..A.B..</c>: the dotted path appears anywhere on segment boundaries.</summary>
    private static string AnySegmentPath(string dottedPath) =>
        "^(?:.*\\.)?" + System.Text.RegularExpressions.Regex.Escape(dottedPath) + "(?:\\..*)?$";
}
