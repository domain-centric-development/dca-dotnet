using System;
using System.Collections.Generic;
using System.Linq;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using ArchUnitNET.Fluent;
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
            OpenHostServicesResideInApiOrIncomingAdapter(),
            OutgoingAdaptersOnlyUseOpenHostServices(),
            IntegrationEventsResideInEventsNamespaces(),
            IntegrationEventsAreRecords(),
            EventListenersUseAntiCorruptionLayer(),
            AtLeastOneBoundedContextIsDeclared(),
            AtLeastOneModuleOwnsALayer(),
            EventsContractInterfacesExtendIntegrationEvent(),
        };
    }

    public string Name => "strategic";

    public IReadOnlyList<IDcaRule> Rules { get; }

    /// <summary>The layout this rule set was built for.</summary>
    public DcaLayout Layout { get; }

    /// <summary>DCA-STR-001.</summary>
    public static IDcaRule DisplayDiscoveredBoundedContexts() =>
        DcaRule.Informational(
            "DCA-STR-001",
            "Diagnostic: Display discovered bounded contexts",
            "Making the discovered contexts visible shows which namespaces the strategic rules govern",
            arch =>
            {
                var observed = new List<string>();
                foreach (var e in arch.BoundedContexts)
                {
                    var description = e.Value.Description.Length > 0 ? " - " + e.Value.Description : "";
                    observed.Add(e.Value.Name + ": " + e.Key + description);
                }

                observed.Add("shared kernel: " + (arch.SharedKernelNamespace ?? "<none>"));
                return observed;
            })
        .Selecting(
            "Every namespace whose marker class carries [BoundedContext], at any depth below the root"
                + " namespace, plus the namespace whose marker class carries [SharedKernel] if there is one."
                + " Modules that own layers without declaring [BoundedContext] are not listed.")
        .Checking(
            "Diagnostic - reports each discovered context's name, namespace and description and the"
                + " shared kernel namespace in the rule's own outcome. It asserts nothing and never fails.");

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
                        Types().That().FollowCustomPredicate(arch.InLayerOf(DcaLayout.Layer.Application, source), "are in the application layer of " + source)
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
                        Types().That().FollowCustomPredicate(arch.InLayerOf(DcaLayout.Layer.Domain, source), "are in the domain layer of " + source)
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
                        Types().That().FollowCustomPredicate(arch.InLayerOf(DcaLayout.Layer.OutgoingAdapter, source), "are in the outgoing adapters of " + source)
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
            "Integration event contracts reside in the configured Events segment",
            "Integration contracts are published separately from translators and transport adapters",
            arch =>
                Types().That().FollowCustomPredicate(t => arch.Layout.Markers.Is(nameof(DcaMarkers.IntegrationEvent), t), "are integration events").And().AreNot(Interfaces())
                    .Should().ResideInNamespaceMatching(AnySegment(Layout.EventsSegment)))
        .Selecting(
            "Non-interface types below the root namespace whose implemented interfaces include"
                + " IIntegrationEvent - directly or through a derived interface; records and record"
                + " structs included, interfaces extending IIntegrationEvent not.")
        .Checking("Every integration-event contract resides in a namespace containing the configured Events segment. Adapter.Outgoing.Event is not an alternative; move contracts to Events or exclude STR-007 during migration. Translators and transport adapters stay separate.");

    /// <summary>DCA-STR-008.</summary>
    public static IDcaRule IntegrationEventsAreRecords() =>
        DcaRule.Check(
            "DCA-STR-008",
            "Integration Events should have immutable shape",
            "An integration event is a published contract: once another context has read it, its shape may only grow, never change under an existing reader. A mutable event cannot make that promise - any holder can rewrite what a second consumer will read",
            arch =>
            {
                var violations = arch.Types
                    .Where(t => t is not Interface && !t.IsCompilerGenerated)
                    .Where(t => arch.Layout.Markers.Is(nameof(DcaMarkers.IntegrationEvent), t))
                    .Where(t => !TacticalPatternRules.IsImmutableShape(t))
                    .Select(t => "Integration event " + t.FullName + " has mutable shape")
                    .ToList();
                DcaRule.Fail(
                    "Integration Events should have immutable shape",
                    violations,
                    "declare the event as a sealed record (or record struct)");
            })
        .Selecting(
            "Non-interface types below the root namespace whose implemented interfaces include"
                + " IIntegrationEvent - directly or through a derived interface; classes, records and structs"
                + " alike, compiler-generated types excluded.")
        .Checking(
            "Classes are sealed or records; structs are allowed. Every inherited instance field is readonly, every property is get-only or init-only and no instance Set*(x): void method exists. Referenced objects and collection contents are not inspected.");

    /// <summary>DCA-STR-010 — documentation only, never fails.</summary>
    public static IDcaRule EventListenersUseAntiCorruptionLayer() =>
        DcaRule.Informational(
            "DCA-STR-010",
            "Diagnostic: Event Listeners consuming integration events should use Anti-Corruption Layer",
            "Consumed integration events are translated into the consuming context's own language before they reach"
                + " its domain — verified by code review, not statically",
            arch => Array.Empty<string>())
        .Selecting("Informational - selects nothing and never fails; it carries doctrine only.")
        .Checking(
            "Nothing is asserted. Whether a consumed integration event is translated into the"
                + " consuming context's own language before it reaches the domain is a code-review"
                + " check.");

    /// <summary>DCA-STR-012.</summary>
    public static IDcaRule AtLeastOneModuleOwnsALayer() =>
        DcaRule.Check(
            "DCA-STR-012",
            "At least one module owns a DCA layer",
            "Without a discovered module root every rule that selects over the layers matches nothing and"
                + " reports success over an empty model",
            arch =>
            {
                if (arch.ModuleRoots().Count > 0)
                {
                    return;
                }

                var segments = arch.LayerSegments().OrderBy(x => x, StringComparer.Ordinal).ToList();
                var observed = arch.Types
                    .Where(t => t.Namespace is not null)
                    .Select(t => t.Namespace!.FullName)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .Take(10)
                    .ToList();

                throw DcaRuleViolationException.Of(
                    $"No namespace below the root namespace '{arch.Layout.RootNamespace}' carries one of the "
                        + $"configured layer segments [{string.Join(", ", segments)}], so no module root was "
                        + "discovered and every rule that selects over the layers - the whole use-case set among "
                        + "them - passes without having looked at anything. Namespaces seen"
                        + (observed.Count < 10 ? "" : " (first ten)")
                        + $": [{string.Join(", ", observed)}].",
                    new[]
                    {
                        "Name the segments this code base uses on the layout: WithDomainSegment(...), "
                            + "WithApplicationSegment(...) and WithAdapterSegment(...).",
                        "Check that the root namespace passed to DcaLayout.ForRootNamespace is the one the modules "
                            + "live under, and that every assembly holding them is passed to Load.",
                        "A code base that deliberately has no layered module switches this rule off with a recorded "
                            + "reason.",
                    });
            })
        .Selecting(
            "The namespaces of the imported types, as a whole - no individual type is reported. A module root is"
                + " any namespace that has a child namespace named after one of the configured layer segments, at"
                + " any depth below the root namespace.")
        .Checking(
            "At least one module root was discovered. The rule says nothing about how many modules there should be"
                + " or how they are cut; it only establishes that the layer-selecting rules have something to look"
                + " at. It is the third guard of the same kind as an empty import and an undeclared bounded"
                + " context.");

    /// <summary>DCA-STR-011.</summary>
    public static IDcaRule AtLeastOneBoundedContextIsDeclared() =>
        DcaRule.Check(
            "DCA-STR-011",
            "At least one bounded context is declared",
            "Without a declared context the context-map and isolation rules select nothing and report success"
                + " over an empty model",
            arch =>
            {
                if (arch.BoundedContexts.Count > 0)
                {
                    return;
                }

                throw DcaRuleViolationException.Of(
                    $"No namespace below the root namespace '{arch.Layout.RootNamespace}' declares "
                        + "[BoundedContext], so every rule that selects over the discovered contexts passes "
                        + "without having looked at anything.",
                    new[]
                    {
                        "Declare the context: a marker class carrying [BoundedContext] directly in the root "
                            + "namespace of each context — a single-context application annotates its root namespace.",
                        "In a multi-project build, check that the test project references every project that holds "
                            + "a context and passes its assembly to Load.",
                        "A code base that deliberately declares no context switches this rule off with a recorded "
                            + "reason; the structural isolation rules keep governing the modules.",
                    });
            })
        .Selecting(
            "The declared bounded contexts of the imported types — every namespace carrying a [BoundedContext]"
                + " marker class, at any depth below the root namespace. No individual type is reported.")
        .Checking(
            "At least one such namespace exists. The rule says nothing about how many contexts there should be,"
                + " about their boundaries, or about modules that own a layer without declaring a context — those"
                + " are governed structurally and are not a substitute for the declaration.");

    /// <summary>
    /// DCA-STR-013. A consumer may publish an interface in its own events namespace that a producer's
    /// integration event implements (interface inversion); the consumer then listens to its own type.
    /// Without the integration event contract on that interface, the consumer cannot read the identity
    /// it needs to recognise a redelivered event.
    /// </summary>
    public IDcaRule EventsContractInterfacesExtendIntegrationEvent() =>
        DcaRule.Check(
            "DCA-STR-013",
            "Interfaces in a module's events namespace must extend IIntegrationEvent",
            "Everything in the events namespace is a published event contract. An interface there is"
                + " one a producer's integration event implements and a consumer receives; delivery"
                + " is at least once, so the consumer needs the event's identity and occurrence time"
                + " through that interface, not only the payload",
            arch =>
            {
                var violations = new List<string>();
                foreach (var root in arch.ModuleRoots())
                {
                    var events = root + "." + Layout.EventsSegment;
                    foreach (var type in arch.Interfaces)
                    {
                        var ns = type.Namespace?.FullName ?? string.Empty;
                        var inEvents = ns == events || ns.StartsWith(events + ".", StringComparison.Ordinal);
                        if (inEvents && !type.IsNested && !type.IsCompilerGenerated && !IsRole(arch, type, nameof(DcaMarkers.IntegrationEvent)))
                        {
                            violations.Add(type.FullName + " is an event contract without the integration event identity");
                        }
                    }
                }

                DcaRule.Fail(
                    "Interfaces in the events namespace extend IIntegrationEvent, so a consumer reads"
                        + " EventId and OccurredOn from the contract it listens to.",
                    violations.Distinct().OrderBy(v => v, StringComparer.Ordinal).ToList(),
                    "let the interface extend IIntegrationEvent");
            })
        .Selecting(
            "Top-level interfaces in <module>.Events and below (segment name from the layout) of"
                + " every module root, the shared kernel's included. Nested interfaces are not selected - a nested interface belongs to its"
                + " enclosing contract.")
        .Checking(
            "The interface is assignable to IIntegrationEvent, directly or through another"
                + " interface. Which classes implement it and whether they live in another module"
                + " are not checked; DCA-ADV-005 and DCA-STR-008 cover the implementing class. An"
                + " empty selection passes.");

    /// <summary>Assignability by role name; reflection decides where the runtime type is available.</summary>
    private static bool IsRole(DcaArchitecture arch, IType type, string role)
    {
        var runtime = arch.RuntimeType(type);
        return runtime is not null ? arch.Layout.Markers.Is(role, runtime) : arch.Layout.Markers.Is(role, type);
    }

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
