using System;
using System.Collections.Generic;
using System.Linq;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

using ArchUnitNET.Domain;
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
            })
        .Selecting(
            "Every namespace whose marker class carries [BoundedContext], at any depth below the root"
                + " namespace, plus the namespace whose marker class carries [SharedKernel] if there is one."
                + " Modules that own layers without declaring [BoundedContext] are not listed.")
        .Checking(
            "Diagnostic - prints each discovered context's name, namespace and description and the"
                + " shared kernel namespace to standard output. It asserts nothing and never fails.");

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
                var perContext = arch.BoundedContexts.Keys.Select(ctx =>
                    (IArchRule)Types().That().ResideInNamespaceMatching(DcaLayout.Below(sharedKernel))
                        .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(DcaLayout.Below(ctx))
                        .Because("Shared Kernel must not depend on bounded context " + arch.ContextName(ctx)));
                DcaRule.EvaluateAll(perContext, arch, title, rationale);
            })
        .Selecting(
            "Types in the namespace whose marker class carries [SharedKernel] and all its sub-namespaces."
                + " When no shared kernel is declared nothing is selected and the rule passes.")
        .Checking(
            "No selected type depends on a type in a namespace carrying [BoundedContext] or below"
                + " it, checked once per declared context and reported together. A module that"
                + " owns layers without declaring"
                + " [BoundedContext] is not a forbidden target here. Dependencies on the root namespace"
                + " outside any context, on infrastructure and on third-party code are not checked.");
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
            })
        .Selecting(
            "Types in <module>.Application and below of every isolated module root - every namespace"
                + " below the root namespace that owns a Domain, Application or Adapter namespace, declared"
                + " as a bounded context or not, the shared kernel excluded. A module that is the only"
                + " isolated root has no foreign target and is skipped.")
        .Checking(
            "No selected type depends on any type in another isolated module root or below it"
                + " (<other> and below), the other module's Api and Events namespaces included. A dependency"
                + " is any reference - field, parameter, return type, record component, type"
                + " argument or call - not only a method call. Dependencies on the shared kernel,"
                + " on infrastructure and on third-party code are not checked. Violations are"
                + " collected per module and reported together.");
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
            })
        .Selecting(
            "Types in <module>.Domain and below of every isolated module root - every namespace below the"
                + " root namespace that owns a Domain, Application or Adapter namespace, declared as a"
                + " bounded context or not, the shared kernel excluded. A module that is the only"
                + " isolated root has no foreign target and is skipped.")
        .Checking(
            "No selected type depends on any type in another isolated module root or below it"
                + " (<other> and below) - not even on its published Api or Events namespaces. Dependencies"
                + " on the shared kernel, on the module's own Application, Adapter and"
                + " Infrastructure namespaces and on third-party code are not checked by this rule."
                + " Violations are collected per module and reported together.");
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
                        AnySegmentPath(Layout.AdapterSegment + "." + Layout.IncomingSegment))))
        .Selecting(
            "Types - classes or interfaces - carrying [OpenHostService] anywhere below the root"
                + " namespace, in any module or none.")
        .Checking(
            "Each resides in a namespace whose path contains the configured Api segment or"
                + " the configured incoming-adapter segments (Adapter.Incoming), at any depth"
                + " and in any sub-namespace. One attributed in a Domain, Application or"
                + " outgoing-adapter namespace is reported. Which module publishes it, and whether"
                + " anyone consumes it, is not checked.");

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
            })
        .Selecting(
            "Types in <module>.Adapter.Outgoing and below of every isolated module root - every namespace"
                + " below the root namespace that owns a Domain, Application or Adapter namespace,"
                + " declared as a bounded context or not, the shared kernel excluded. A module that"
                + " is the only isolated root has no foreign target and is skipped.")
        .Checking(
            "No selected type depends on a type in another isolated module root (<other> and below)"
                + " unless that type lives in the other module's published namespaces <other>.Api"
                + " or <other>.Events and below (segment names from the layout). The other module's Domain,"
                + " Application, Adapter and Infrastructure namespaces are internal and reported. The"
                + " allow-list is the namespace convention alone - no framework attribute is read."
                + " Dependencies on the shared kernel and on third-party code are not checked.");
    }

    /// <summary>DCA-STR-007.</summary>
    public IDcaRule IntegrationEventsResideInEventsNamespaces() =>
        DcaRule.Of(
            "DCA-STR-007",
            "Integration Events must be in Events or adapter outgoing event namespaces",
            "Integration Events must be in Events/ namespaces (published named interface) or Adapter.Outgoing.Event/ namespaces",
            arch =>
                Types().That().ImplementInterface(typeof(IIntegrationEvent)).And().AreNot(Interfaces())
                    .Should().ResideInNamespaceMatching(AnyOf(AnySegment(Layout.EventsSegment), OutgoingEventAdapterPattern())))
        .Selecting(
            "Non-interface types below the root namespace whose implemented interfaces include"
                + " IIntegrationEvent - directly or through a derived interface; records and record"
                + " structs included, interfaces extending IIntegrationEvent not.")
        .Checking(
            "Each resides in a namespace whose path contains the configured Events segment"
                + " or Adapter.Outgoing.Event - the trailing Event segment is"
                + " fixed, not configurable. An integration event in a Domain or Application"
                + " namespace is reported.");

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
                var violations = arch.Types
                    .Where(t => t is not Interface && !t.IsCompilerGenerated)
                    .Where(t => t.ImplementedInterfaces.Any(i => i.FullName == typeof(IIntegrationEvent).FullName))
                    .Where(t => !TacticalPatternRules.IsRecordLike(t))
                    .Select(t => "Integration event " + t.FullName + " is not a record")
                    .ToList();
                DcaRule.Fail(
                    "Integration Events should be immutable records",
                    violations,
                    "declare the event as a sealed record (or record struct)");
            })
        .Selecting(
            "Non-interface types below the root namespace whose implemented interfaces include"
                + " IIntegrationEvent - directly or through a derived interface; classes, records and structs"
                + " alike, compiler-generated types excluded.")
        .Checking(
            "The type is a record class or a struct (a record struct is a struct in the model and counts)."
                + " A sealed class with init-only properties does not - only the record form is accepted."
                + " The components' own immutability is not checked.");

    /// <summary>DCA-STR-009.</summary>
    public static IDcaRule AntiCorruptionLayerComponentsResideInAclNamespaces() =>
        DcaRule.Of(
            "DCA-STR-009",
            "Anti-Corruption Layer components must be in Acl namespaces",
            "Anti-Corruption Layer components must be in 'Acl' namespaces for clear architectural intent (DDD Strategic Pattern)",
            arch =>
                Types().That().HaveNameMatching("(EventTranslator|ACL|AntiCorruptionLayer)$")
                    .Should().ResideInNamespaceMatching(AnySegment("Acl")))
        .Selecting(
            "Types anywhere below the root namespace whose name ends with"
                + " EventTranslator, ACL or AntiCorruptionLayer - selected by name alone, no marker"
                + " or attribute is read.")
        .Checking(
            "Each resides in a namespace whose path contains an Acl segment, at any depth."
                + " A translation class named otherwise is neither selected nor checked.");

    /// <summary>DCA-STR-010 — documentation only, never fails.</summary>
    public static IDcaRule EventListenersUseAntiCorruptionLayer() =>
        DcaRule.Check(
            "DCA-STR-010",
            "Event Listeners consuming integration events should use Anti-Corruption Layer",
            "Consumed integration events are translated into the consuming context's own language before they reach"
                + " its domain — verified by code review, not statically",
            arch => { })
        .Selecting("Informational - selects nothing and never fails; it carries doctrine only.")
        .Checking(
            "Nothing is asserted. Whether a consumed integration event is translated into the"
                + " consuming context's own language before it reaches the domain is a code-review"
                + " check.");

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
