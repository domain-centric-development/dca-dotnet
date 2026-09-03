using System;
using System.Collections.Generic;
using System.Linq;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

using ArchUnitNET.Fluent;

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
            OpenHostServicesResideInApiOrIncomingAdapter(),
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

    /// <summary>
    /// DCA-STR-003. Selects over every module that owns a DCA layer (<see cref="DcaArchitecture.IsolatedModuleRoots"/>),
    /// declared as a bounded context or not — on the source side and on the target side. A module must not be able
    /// to escape isolation, or to have its internals reached into, by staying off the context map.
    /// </summary>
    public IDcaRule ApplicationLayerIsolation()
    {
        const string title = "Modules must not access each other in the application layer";
        const string rationale = "An application layer talks to other modules through its own output ports, implemented by"
            + " adapters - never directly. Selects structurally over every module that owns a DCA"
            + " layer, declared as a bounded context or not: an undeclared module must not be able"
            + " to escape isolation by staying off the context map";
        return DcaRule.Check(
            "DCA-STR-003",
            title,
            rationale,
            arch =>
            {
                var perModule = new List<IArchRule>();
                foreach (var source in arch.IsolatedModuleRoots())
                {
                    var forbidden = arch.ModuleRootPatternsExcluding(source);
                    if (forbidden.Length == 0)
                    {
                        continue;
                    }

                    // Dependencies, not accesses: a field, parameter or record component of a foreign
                    // type is a dependency even without a method call.
                    perModule.Add(
                        Types().That().ResideInNamespaceMatching(Layout.ApplicationPatternOf(source))
                            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(AnyOf(forbidden))
                            .Because("The application layer of module '" + arch.ContextName(source)
                                + "' must not access other modules directly - define output ports and use adapters instead"));
                }

                DcaRule.EvaluateAll(perModule, arch, title, rationale);
            });
    }

    /// <summary>DCA-STR-004.</summary>
    public IDcaRule DomainLayerIsolation()
    {
        const string title = "Modules must not access each other in the domain layer";
        const string rationale = "A domain layer talks to its own module and the shared kernel, nothing else - not even"
            + " another module's api/. Selects structurally over every module that owns a DCA layer,"
            + " declared as a bounded context or not";
        return DcaRule.Check(
            "DCA-STR-004",
            title,
            rationale,
            arch =>
            {
                var perModule = new List<IArchRule>();
                foreach (var source in arch.IsolatedModuleRoots())
                {
                    var forbidden = arch.ModuleRootPatternsExcluding(source);
                    if (forbidden.Length == 0)
                    {
                        continue;
                    }

                    perModule.Add(
                        Types().That().ResideInNamespaceMatching(Layout.DomainPatternOf(source))
                            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(AnyOf(forbidden))
                            .Because("The domain layer of module '" + arch.ContextName(source)
                                + "' must depend on nothing outside its own module and the shared kernel"));
                }

                DcaRule.EvaluateAll(perModule, arch, title, rationale);
            });
    }

    /// <summary>
    /// DCA-STR-005. An Open Host Service is a relationship pattern, not a transport: the published protocol one
    /// context offers to many consumers. In-process it is the <c>Api</c> namespace; over the network it is an
    /// incoming adapter (REST, gRPC, MCP, ...). Which sub-namespace of the incoming adapter it sits in is the
    /// project's business.
    /// </summary>
    public IDcaRule OpenHostServicesResideInApiOrIncomingAdapter() =>
        DcaRule.Of(
            "DCA-STR-005",
            "Open Host Services must be published: in the api package or as an incoming adapter",
            "An Open Host Service is the protocol a context publishes for other contexts - in-process"
                + " as its api/ package, over the network as an incoming adapter (REST, gRPC, MCP). It"
                + " belongs at the context boundary, never in the domain or application layer; the"
                + " adapter's sub-package is irrelevant",
            arch =>
                Types().That().HaveAnyAttributes(typeof(OpenHostServiceAttribute))
                    .Should().ResideInNamespaceMatching(AnyOf(
                        AnySegment(Layout.ApiSegment),
                        AnySegmentPath(Layout.AdapterSegment + "." + Layout.IncomingSegment))));

    /// <summary>
    /// DCA-STR-006. The allow-list is the namespace convention <c>Api</c> / <c>Events</c> of the target module —
    /// DCA's in-process contract, a convention of the architecture and not of any framework. Everything else in
    /// a foreign module (its domain, application, adapters, infrastructure) is internal.
    /// </summary>
    public IDcaRule OutgoingAdaptersOnlyUseOpenHostServices()
    {
        const string title = "Outgoing adapters accessing other modules must only use their published api/ and events/"
            + " packages";
        const string rationale = "Cross-module communication goes through the target's published api/ (synchronous) and"
            + " events/ (asynchronous) packages - DCA's in-process contract convention, package"
            + " names rather than framework annotations - never through its domain, application,"
            + " adapter or infrastructure packages. Selects structurally over every module that owns"
            + " a DCA layer, declared as a bounded context or not";
        return DcaRule.Check(
            "DCA-STR-006",
            title,
            rationale,
            arch =>
            {
                var perModule = new List<IArchRule>();
                foreach (var source in arch.IsolatedModuleRoots())
                {
                    var foreign = arch.ModuleRootPatternsExcluding(source);
                    if (foreign.Length == 0)
                    {
                        continue;
                    }

                    // Foreign internals = anything in another module except its published Api/Events namespaces.
                    var internals = "(?!" + AnyOf(arch.PublishedPatternsExcluding(source)) + ")(?:" + AnyOf(foreign) + ")";
                    perModule.Add(
                        Types().That().ResideInNamespaceMatching(Layout.OutgoingAdapterPatternOf(source))
                            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(internals)
                            .Because("Outgoing adapters in module '" + arch.ContextName(source)
                                + "' must not access another module's internals - use its api/ or events/ packages instead"));
                }

                DcaRule.EvaluateAll(perModule, arch, title, rationale);
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
                    .Should().ResideInNamespaceMatching(AnyOf(AnySegment(Layout.EventsSegment), OutgoingEventAdapterPattern())));

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
    private static string AnyOf(params string[] patterns) => DcaLayout.AnyOf(patterns);

    /// <summary>ArchUnit <c>..Segment..</c>: the segment appears anywhere in the namespace.</summary>
    private static string AnySegment(string segment) => AnySegmentPath(segment);

    /// <summary>ArchUnit <c>..A.B..</c>: the dotted path appears anywhere on segment boundaries.</summary>
    private static string AnySegmentPath(string dottedPath) => DcaLayout.AnySegmentPath(dottedPath);
}
