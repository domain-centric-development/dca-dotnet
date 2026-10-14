using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Conditions;
using ArchUnitNET.Domain.Extensions;
using System.Reflection;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace DomainCentric.ArchRules.Rules;

/// <summary>
/// Hexagonal Architecture (Ports and Adapters) rules: separation between ports and adapters,
/// incoming adapters drive the application through its input ports, outgoing adapters implement
/// outbound ports, adapters never talk to each other directly, incoming adapters stay inside their
/// own bounded context and obtain no domain collaborator of their own, and outgoing adapters never
/// drive the application.
/// </summary>
public sealed class HexagonalRules : IDcaRuleSet
{
    /// <summary>Java rules of this set that have no .NET reading (none).</summary>
    public static readonly IReadOnlyDictionary<string, string> NotApplicable = new Dictionary<string, string>();

    public HexagonalRules(DcaLayout layout)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Rules = new List<IDcaRule>
        {
            DomainMustNotAccessAdapters(),
            ApplicationMustNotAccessAdapters(),
            ControllersMustNotAccessRepositories(),
            IncomingAdaptersMustNotUseInfrastructureImplementations(),
            OutgoingAdaptersMustNotUseInfrastructureImplementations(),
            AdaptersMustNotCommunicateDirectly(),
            IncomingAdaptersStayInOwnContext(),
            RepositoryClassesResideInOutgoingAdapter(),
            SharedOutputPortsExtendOutputPort(),
            OutputPortsMustNotResideInDomain(),
            IncomingAdaptersMustDependOnInputPortsNotUseCaseClasses(),
            IncomingAdaptersMustNotDependOnDomainServices(),
            OutgoingAdaptersMustNotDependOnInputPorts(),
            GetHandlersMustNotDriveCommands(),
            AdaptersPublishNoEnumMemberName(),
        }.AsReadOnly();
    }

    public string Name => "hexagonal";

    public IReadOnlyList<IDcaRule> Rules { get; }

    /// <summary>The layout this rule set was built for.</summary>
    public DcaLayout Layout { get; }

    /// <summary>Pattern of event consumers - the incoming adapters that react to other modules' integration events; every segment comes from the layout.</summary>
    private string EventConsumerPattern() => Layout.IncomingEventAdapterPattern;

    /// <summary>Regular expression matching any of the given namespace patterns.</summary>
    internal static string AnyOf(IEnumerable<string> patterns) => DcaLayout.AnyOf(patterns);

    /// <summary>
    /// Whether a class is a controller: it derives from the configured controller/page-model base, carries
    /// the API-controller attribute, or its name ends with <c>Controller</c> or the layout's controller suffix.
    /// </summary>
    internal static bool IsController(Class type, DcaLayout layout) =>
        type.Name.EndsWith(layout.ControllerSuffix, StringComparison.Ordinal)
        || type.Name.EndsWith(layout.RestControllerSuffix, StringComparison.Ordinal)
        || (FrameworkTypes.IsSet(layout.FrameworkTypes.ControllerBase) && type.IsAssignableTo(layout.FrameworkTypes.ControllerBase))
        || (FrameworkTypes.IsSet(layout.FrameworkTypes.PageModelBase) && type.IsAssignableTo(layout.FrameworkTypes.PageModelBase))
        || (FrameworkTypes.IsSet(layout.FrameworkTypes.ApiControllerAttribute)
            && type.Attributes.Any(a => a.FullName == layout.FrameworkTypes.ApiControllerAttribute));

    public IDcaRule DomainMustNotAccessAdapters() =>
        DcaRule.Of(
            "DCA-HEX-001",
            "Classes from the domain should not access port adapters",
            "Domain should not depend on adapters (ports and adapters pattern)",
            arch => Types().That().ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllDomainModelPatterns()))
                .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllAdapterPatterns())))
        .Selecting(
            "Types in <module>.Domain.Model of every module root - the domain model"
                + " namespace, not the whole domain layer.")
        .Checking(
            "No dependency on a type in <module>.Adapter of any module root, incoming or"
                + " outgoing. Domain types outside the model namespace (domain services, events) are"
                + " not selected. An empty selection passes.");

    public IDcaRule ApplicationMustNotAccessAdapters() =>
        DcaRule.Of(
            "DCA-HEX-002",
            "Application Services should not access port adapters",
            "Application services should only depend on domain and outbound ports, not adapters",
            arch => Types().That().ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllApplicationPatterns()))
                .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllAdapterPatterns())))
        .Selecting(
            "Types in <module>.Application of every module root, Application.Shared"
                + " included.")
        .Checking(
            "No dependency on a type in <module>.Adapter of any module root. An empty"
                + " selection passes.");

    public IDcaRule ControllersMustNotAccessRepositories() =>
        DcaRule.Of(
            "DCA-HEX-003",
            "Controllers and Resources must never access repositories directly",
            "Controllers must go through use cases (input ports), never directly to repositories",
            arch => Classes().That().FollowCustomPredicate(c => IsController(c, Layout), "are controllers")
                .Should().NotDependOnAnyTypesThat()
                .FollowCustomPredicate(t => t.IsAssignableTo(arch.Layout.Markers.Repository), "are repositories"))
        .Selecting(
            "Controller classes anywhere in the loaded assemblies: a class whose name ends with"
                + " the configured controller suffix or the configured REST-controller suffix, one deriving"
                + " from the configured controller or page-model base class, or one carrying the"
                + " configured API-controller attribute. Not restricted to adapter namespaces.")
        .Checking(
            "No dependency on a type assignable to IRepository - the port interface or an"
                + " implementation. Other output ports (IStore, event publishers) are not checked; a"
                + " controller that reaches a repository through another class is not reported. An"
                + " empty selection passes.");

    public IDcaRule IncomingAdaptersMustNotUseInfrastructureImplementations() =>
        DcaRule.Of(
            "DCA-HEX-004",
            "Incoming Adapters must only use outbound ports (not infrastructure implementations)",
            "Incoming adapters should only use outbound ports declared as interfaces (Ports.Out), not"
                + " infrastructure implementation details",
            arch => Types().That().ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllIncomingAdapterPatterns()))
                .Should().NotDependOnAnyTypesThat()
                .FollowCustomPredicate(arch.IsInfrastructureImplementation, "are infrastructure implementations"))
        .Selecting(
            "Types in <module>.Adapter.Incoming of every module root.")
        .Checking(
            "No dependency on a type in an infrastructure namespace: the global"
                + " Root.Infrastructure or an isolated module's own <module>.Infrastructure, the"
                + " namespace itself or any sub-namespace with an exact segment boundary. The shared"
                + " kernel's infrastructure namespace does not count as an infrastructure"
                + " implementation. An empty selection passes.");

    public IDcaRule OutgoingAdaptersMustNotUseInfrastructureImplementations() =>
        DcaRule.Check("DCA-HEX-005", "Outgoing adapters must not use another module's infrastructure",
            "Technical infrastructure reuse preserves module isolation",
            arch => DcaRule.Fail("Another module's infrastructure is forbidden",
                arch.Types.Where(t => Regex.IsMatch(t.Namespace?.FullName ?? "", DcaLayout.AnyOf(arch.AllOutgoingAdapterPatterns())))
                    .SelectMany(adapter => adapter.Dependencies.Select(d => d.Target)
                        .Where(target => arch.IsInfrastructureImplementation(target)
                            && arch.IsolatedModuleRoots().Any(root => root != arch.ModuleRootOf(adapter.Namespace?.FullName ?? "")
                                && DcaLayout.IsBelow(target.Namespace?.FullName ?? "", root + "." + Layout.InfrastructureSegment)))
                        .Select(target => $"{adapter.FullName} depends on {target.FullName}")).ToList()))
        .Selecting("Types in every module's outgoing adapter namespace.")
        .Checking("Global and own-module infrastructure dependencies pass; another module's infrastructure fails. Boundaries use exact namespace segments.");

    public IDcaRule AdaptersMustNotCommunicateDirectly() =>
        DcaRule.Of(
            "DCA-HEX-006",
            "Incoming port adapters must not depend directly on outgoing port adapters"
                + " within the same context",
            "Port adapters should communicate through application services, not directly (event"
                + " consumers are the exception)",
            arch => Types().That().ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllIncomingAdapterPatterns()))
                .And().DoNotResideInNamespaceMatching(EventConsumerPattern())
                .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllOutgoingAdapterPatterns())))
        .Selecting(
            "Types in <module>.Adapter.Incoming of every module root, excluding those"
                + " below the configured event-consumer segment (Adapter.Incoming.Event by default).")
        .Checking(
            "No dependency on a type in <module>.Adapter.Outgoing of any module root. The"
                + " reverse direction (an outgoing adapter using an incoming one) and dependencies"
                + " between two incoming or two outgoing adapters are not checked. An empty"
                + " selection passes.");

    /// <summary>
    /// DCA-HEX-007. Structural, over every module that owns a DCA layer (<see cref="DcaArchitecture.IsolatedModuleRoots"/>),
    /// declared as a bounded context or not — so an undeclared module can neither reach out nor be reached into.
    /// </summary>
    public IDcaRule IncomingAdaptersStayInOwnContext()
    {
        const string title = "Incoming adapters depend on no other module, except event consumers on the events"
            + " they subscribe to";
        const string rationale = "An incoming adapter drives its own module only: a controller that needs a sibling's"
            + " data goes through its own use case, output port and outgoing adapter to the sibling's published api"
            + " - only a subscription legitimately meets a foreign contract on the incoming side";
        return DcaRule.Check(
            "DCA-HEX-007",
            title,
            rationale,
            arch =>
            {
                var perModule = new List<IArchRule>();
                foreach (var module in arch.IsolatedModuleRoots())
                {
                    var otherModules = arch.ModuleRootPatternsExcluding(module);
                    if (otherModules.Length == 0)
                    {
                        continue;
                    }

                    perModule.Add(
                        Types().That().ResideInNamespaceMatching(Layout.IncomingAdapterPatternOf(module))
                            .And().DoNotResideInNamespaceMatching(EventConsumerPattern())
                            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(AnyOf(otherModules))
                            .Because("Incoming adapters in module '" + arch.ContextName(module)
                                + "' drive their own module only - reach a sibling through an own use case, output port and outgoing adapter"));
                }

                DcaRule.EvaluateAll(perModule, arch, title, rationale);
            })
            .Selecting(
                "Per isolated module root - every module root except the shared kernel, declared"
                + " a bounded context or not: types in <module>.Adapter.Incoming, excluding"
                + " those below the configured event-consumer segment (Adapter.Incoming.Event by"
                + " default), which are the exemption. A module that is the only isolated module is skipped.")
            .Checking(
                "No dependency on any type in another isolated module root (<other> and below), its"
                + " published Api and Events namespaces included - the allow-list DCA-STR-006 grants"
                + " outgoing adapters does not apply on the incoming side. Event consumers are not"
                + " selected and so may depend on the events they subscribe to. Dependencies on the"
                + " shared kernel and on namespaces outside every module root are not checked."
                + " Findings of all modules are collected and reported together; a module without"
                + " incoming adapters passes.");
    }

    public IDcaRule RepositoryClassesResideInOutgoingAdapter() =>
        DcaRule.Of(
            "DCA-HEX-008",
            "Classes named *Repository must reside in the outgoing adapter namespace (name-based discovery of unmarked repositories)",
            "Repository implementations are secondary adapters (outgoing ports)",
            arch => Classes().That().HaveNameEndingWith("Repository")
                .Should().ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllOutgoingAdapterPatterns())))
        .Selecting(
            "Classes anywhere in the loaded assemblies whose name ends with Repository -"
                + " implementations and abstract base classes alike. The IRepository port interfaces"
                + " themselves are not selected.")
        .Checking(
            "Each resides in <module>.Adapter.Outgoing of some module root. Whether the"
                + " class implements an IRepository port is not checked - only the name is. An empty"
                + " selection passes.");

    public IDcaRule SharedOutputPortsExtendOutputPort() =>
        DcaRule.Of(
            "DCA-HEX-009",
            "Output Ports in Application.Shared must extend IOutputPort",
            "Top-level interfaces in Application.Shared are output ports and must extend IOutputPort to"
                + " be part of the port hierarchy. Nested interfaces (e.g. IIdentityProvider.Identity)"
                + " are part of their enclosing port's contract, not ports themselves",
            arch => Interfaces().That().ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllSharedOutputPortPatterns()))
                .And().FollowCustomPredicate(i => !i.IsNested, "are top-level interfaces")
                .Should().FollowCustomCondition(
                    i => new ConditionResult(i, i.IsAssignableTo(arch.Layout.Markers.OutputPort), $"{i.FullName} is not assignable to {arch.Layout.Markers.OutputPort}"),
                    "be assignable to the configured output-port marker"))
        .Selecting(
            "Top-level interfaces in <module>.Application.Shared of every module root."
                + " Nested interfaces are not selected - they belong to their enclosing port's"
                + " contract.")
        .Checking(
            "The interface is assignable to IOutputPort, directly or through IRepository,"
                + " IStore, IDomainEventPublisher, IIntegrationEventPublisher or another IOutputPort"
                + " sub-interface. Classes, records and enums in Application.Shared are not checked."
                + " An empty selection passes. The rule presumes the vocabulary has a common port"
                + " root: OutputPort must be set whenever any port role is, because every port of"
                + " the project is measured against it. A vocabulary that has no such root has"
                + " nothing this rule can assert - switch the id off rather than pointing the role"
                + " at an unrelated type.");

    public IDcaRule OutputPortsMustNotResideInDomain() =>
        DcaRule.Of(
            "DCA-HEX-010",
            "Output ports must not reside in the domain layer",
            "output ports (IRepository, IStore, IOutputPort) are an application-layer concern and must live"
                + " in Application/Shared/, not Domain/",
            arch => Interfaces().That().FollowCustomPredicate(i => i.IsAssignableTo(arch.Layout.Markers.OutputPort), "are output ports")
                .Should().NotResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllDomainPatterns())))
        .Selecting(
            "Interfaces anywhere in the loaded assemblies that are assignable to"
                + " IOutputPort - the building-block port interfaces themselves included.")
        .Checking(
            "None resides in <module>.Domain of any module root. Classes implementing an"
                + " output port are not selected; where an interface outside the domain has to live"
                + " is not checked here. An empty selection passes.");

    public IDcaRule IncomingAdaptersMustDependOnInputPortsNotUseCaseClasses() =>
        DcaRule.Check(
            "DCA-HEX-011",
            "Incoming Adapters must depend on input port interfaces, not on use case classes",
            "A driving adapter drives the application through its port. Injecting the concrete"
                + " implementation instead couples the adapter to one realisation of the use case,"
                + " defeats the Dependency Inversion Principle the port exists for, and makes the"
                + " adapter untestable without the real use case and everything it depends on",
            arch =>
            {
                var violations = arch.Classes
                    .Where(c => InNamespace(c, DcaLayout.AnyOf(arch.AllIncomingAdapterPatterns())))
                    .SelectMany(c => c.Dependencies
                        .Select(d => d.Target)
                        .Where(t => IsUseCaseImplementation(arch, t))
                        .Select(t => $"{c.FullName} depends on the use case class {t.FullName}"))
                    .Distinct()
                    .OrderBy(v => v, StringComparer.Ordinal)
                    .ToList();
                DcaRule.Fail(
                    "Incoming Adapters must depend on input port interfaces, not on use case classes",
                    violations,
                    "inject the I<UseCaseName>InputPort interface instead of the <UseCaseName> class");
            })
        .Selecting(
            "Classes in <module>.Adapter.Incoming of every module root, event consumers"
                + " included.")
        .Checking(
            "No dependency on a use case implementation: a class assignable to"
                + " IInputPort, directly or through an I*InputPort interface - abstract base classes"
                + " included. Depending on the input port interfaces themselves is what the rule"
                + " expects. An empty selection passes.");

    /// <summary>
    /// DCA-HEX-012. Selects every class in an incoming adapter namespace (event consumers included) and reports each
    /// dependency on a type assignable to <see cref="IDomainService"/> — a field, constructor parameter, member type
    /// or call. Constructor parameters are read from the runtime type as well, because ArchUnitNET attributes the
    /// body of an async member to its state machine. Outgoing adapters are outside the selection.
    /// </summary>
    public IDcaRule IncomingAdaptersMustNotDependOnDomainServices() =>
        DcaRule.Check(
            "DCA-HEX-012",
            "Incoming Adapters must not depend on domain services",
            "An incoming adapter translates external input, calls an input port and formats its result."
                + " Injecting or invoking a domain service bypasses the application boundary; the use case owns"
                + " that collaboration and puts its outcome into the result. Outgoing adapters are outside this"
                + " rule - repositories and other driven adapters may construct or reconstitute domain objects"
                + " while implementing output ports",
            arch =>
            {
                var incoming = DcaLayout.AnyOf(arch.AllIncomingAdapterPatterns());
                var violations = new List<string>();
                foreach (var adapter in arch.Classes.Where(c => InNamespace(c, incoming)))
                {
                    var services = adapter.Dependencies
                        .Select(d => d.Target)
                        .Where(t => !t.IsGenericParameter && IsAssignableTo(arch, t, arch.Layout.Markers.DomainService))
                        .Select(t => t.FullName);
                    var runtime = arch.RuntimeType(adapter);
                    if (runtime is not null)
                    {
                        services = services.Concat(runtime
                            .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .SelectMany(c => c.GetParameters())
                            .Select(p => p.ParameterType)
                            .Where(t => DcaMarkers.IsAssignableToByName(t, arch.Layout.Markers.DomainService))
                            .Select(t => t.FullName!));
                    }

                    violations.AddRange(services.Distinct()
                        .Select(s => $"{adapter.FullName} depends on the domain service {s}"));
                }

                DcaRule.Fail(
                    "Incoming Adapters must not depend on domain services",
                    violations.Distinct().OrderBy(v => v, StringComparer.Ordinal).ToList(),
                    "move the collaboration into the use case and carry its outcome in the result");
            })
        .Selecting(
            "Classes in <module>.Adapter.Incoming of every module root, event consumers"
                + " included. Outgoing adapters are not selected.")
        .Checking(
            "No dependency on a type assignable to IDomainService - the building-block marker"
                + " interface, any sub-interface of it and every class implementing one. A domain"
                + " class without the marker is not a domain service by this rule. Injecting it,"
                + " calling it or naming it in a signature all count as a dependency; constructor"
                + " parameters are read from the runtime type as well, so a service injected into an"
                + " adapter whose members are all async is still found. An empty selection passes.");

    /// <summary>
    /// DCA-HEX-013. The direction of an adapter is decided by who calls whom: an adapter that invokes an input port
    /// drives the application and is incoming, whatever namespace it sits in. Constructor parameters are read from
    /// the runtime type as well, because ArchUnitNET attributes the body of an async member to its state machine.
    /// </summary>
    public IDcaRule OutgoingAdaptersMustNotDependOnInputPorts() =>
        DcaRule.Check(
            "DCA-HEX-013",
            "Outgoing Adapters must not depend on input ports",
            "An outgoing adapter is driven: the application calls it through an output port. One that"
                + " calls an input port drives the application instead - it is an incoming adapter"
                + " filed on the wrong side, and the call it makes runs a second use case from inside"
                + " the first one's outgoing edge",
            arch =>
            {
                var outgoing = DcaLayout.AnyOf(arch.AllOutgoingAdapterPatterns());
                var violations = new List<string>();
                foreach (var adapter in arch.Classes.Where(c => InNamespace(c, outgoing)))
                {
                    var ports = adapter.Dependencies
                        .Select(d => d.Target)
                        .Where(t => !t.IsGenericParameter && IsAssignableTo(arch, t, arch.Layout.Markers.InputPort))
                        .Select(t => t.FullName);
                    var runtime = arch.RuntimeType(adapter);
                    if (runtime is not null)
                    {
                        ports = ports.Concat(runtime
                            .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .SelectMany(c => c.GetParameters())
                            .Select(p => p.ParameterType)
                            .Where(t => DcaMarkers.IsAssignableToByName(t, arch.Layout.Markers.InputPort))
                            .Select(t => t.FullName!));
                    }

                    violations.AddRange(ports.Distinct()
                        .Select(p => $"{adapter.FullName} depends on the input port {p}"));
                }

                DcaRule.Fail(
                    "Outgoing Adapters must not depend on input ports",
                    violations.Distinct().OrderBy(v => v, StringComparer.Ordinal).ToList(),
                    "move the class to the incoming adapter namespace, or let the use case reach the"
                        + " capability through an output port");
            })
        .Selecting("Classes in <module>.Adapter.Outgoing of every module root.")
        .Checking(
            "No dependency on a type assignable to IInputPort - the input port interfaces and the use"
                + " case classes implementing them, of any module. Calling another module's published"
                + " api is not a dependency on an input port, even where that api delegates to one"
                + " - only direct dependencies are read; constructor parameters are read from the"
                + " runtime type as well, so a port injected into an adapter whose members are all"
                + " async is still found. An empty selection passes.");

    /// <summary>
    /// DCA-HEX-014 (experimental). A state-changing use case is reached only by an unsafe method. Handlers are found
    /// through the GET-mapping roles of the framework preset; the call is followed through the class's own helpers.
    /// </summary>
    public IDcaRule GetHandlersMustNotDriveCommands() =>
        DcaRule.Check(
            "DCA-HEX-014",
            "GET handlers must not drive an input port that takes a command",
            "A state-changing use case is reached only by an unsafe HTTP method, never by GET; links do not"
                + " create state. Prefetching, crawlers and cross-site navigation follow a link without asking,"
                + " and would otherwise trigger the change",
            arch =>
            {
                var roles = arch.Layout.FrameworkTypes;
                var incoming = DcaLayout.AnyOf(arch.AllIncomingAdapterPatterns());
                var violations = new List<string>();
                foreach (var adapter in arch.Classes.Where(c => !c.IsCompilerGenerated && InNamespace(c, incoming)).OrderBy(c => c.FullName, StringComparer.Ordinal))
                {
                    var runtime = arch.RuntimeType(adapter);
                    if (runtime is null)
                    {
                        continue;
                    }

                    var byPrefix = IsController(adapter, arch.Layout);
                    var graph = new IntraClassCalls(runtime);
                    foreach (var handler in graph.Units.OfType<MethodInfo>()
                        .Where(m => m.DeclaringType == runtime && IsGetHandler(m, roles, byPrefix)))
                    {
                        foreach (var call in graph.ReachableFrom(handler).SelectMany(IntraClassCalls.IlCalls))
                        {
                            var command = CommandTakenBy(call, arch.Layout.Markers);
                            if (command is not null)
                            {
                                violations.Add($"{adapter.FullName}.{handler.Name} handles GET and drives"
                                    + $" {call.DeclaringType!.Name.Split('`')[0]}.{call.Name} with the command {command}");
                            }
                        }
                    }
                }

                DcaRule.Fail(
                    "GET handlers must not drive an input port that takes a command",
                    violations.Distinct().ToList(),
                    "map the handler to POST, PUT or DELETE, or let the GET handler ask a query");
            })
        .Experimental()
        .Selecting(
            "Methods declared by classes in <module>.Adapter.Incoming of every module root that carry an"
                + " attribute of the configured GET-mapping role (derived attributes included), or - on a"
                + " class recognised as a controller or page model - whose name starts with a configured"
                + " GET-handler prefix. Both roles are empty in the None() preset, which then selects nothing.")
        .Checking(
            "No method reached from the handler through calls within the class - async state machines and"
                + " lambdas included - calls a method of a type assignable to IInputPort that takes a"
                + " parameter whose type name ends in Command. A handler mapped by a routing call rather than"
                + " by an attribute or a name is not seen, and a command reached through a second class is"
                + " not followed. An empty selection passes.");

    /// <summary>
    /// DCA-HEX-016 (experimental, informational). A value an adapter publishes from an enum is spelled explicitly,
    /// never derived from the member's name. Read from the IL: a <c>ToString</c> on the enum (through
    /// <c>constrained.</c> or after <c>box</c>), a boxed enum passed into a string-building call, an interpolation
    /// hole of the enum type, and <c>Enum.GetName</c> instantiated over it.
    /// </summary>
    public IDcaRule AdaptersPublishNoEnumMemberName() =>
        DcaRule.Informational(
            "DCA-HEX-016",
            "Diagnostic: adapters that publish an enum by its member name",
            "A value an adapter publishes from an enum is spelled explicitly - a code per value on the enum or"
                + " a mapping in the adapter - never derived from the member's name: renaming a member is a"
                + " refactoring inside the code and must not change what a client reads",
            arch =>
            {
                var inner = new Regex(DcaLayout.AnyOf(arch.AllDomainPatterns().Concat(arch.AllApplicationPatterns())));
                bool InnerEnum(Type? type) => type is { IsEnum: true, Namespace: { } ns } && inner.IsMatch(ns);

                var adapters = DcaLayout.AnyOf(arch.AllAdapterPatterns());
                var observed = new List<string>();
                foreach (var adapter in arch.Types.Where(t => InNamespace(t, adapters)).OrderBy(t => t.FullName, StringComparer.Ordinal))
                {
                    var runtime = arch.RuntimeType(adapter);
                    if (runtime is null || runtime.IsNested)
                    {
                        continue;
                    }

                    // A record's generated PrintMembers/ToString spells every member - not the adapter's choice.
                    foreach (var unit in new IntraClassCalls(runtime).Units.Where(u => !u.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false)))
                    {
                        Type? pending = null;
                        foreach (var (code, operand) in IntraClassCalls.IlInstructions(unit))
                        {
                            if ((code == System.Reflection.Emit.OpCodes.Box || code == System.Reflection.Emit.OpCodes.Constrained) && operand is Type boxed)
                            {
                                pending = InnerEnum(boxed) ? boxed : null;
                                continue;
                            }

                            if (operand is not MethodBase call)
                            {
                                continue;
                            }

                            var how = EnumNameUse(call, pending, InnerEnum, out var spelled);
                            pending = null;
                            if (how is not null)
                            {
                                observed.Add($"{adapter.FullName}.{IntraClassCalls.DisplayName(unit)} {how} the enum {spelled!.FullName}");
                            }
                        }
                    }
                }

                return observed.Distinct().ToList();
            })
        .Experimental()
        .Selecting(
            "Non-nested types in <module>.Adapter of every module root, incoming and outgoing, with their"
                + " runtime type in the loaded assemblies, together with their nested, async state-machine and"
                + " closure types; enums declared in a domain or application namespace of any module root.")
        .Checking(
            "Informational diagnostic only: lists a method that turns such an enum into its member name as"
                + " the IL shows it - ToString in any overload, called through a constrained call or on the"
                + " boxed value; the boxed value passed straight into a string-building call (concatenation,"
                + " formatting, joining, appending); an interpolation hole of the enum type; Enum.GetName or"
                + " Enum.GetNames over it - and never fails. Compiler-generated methods, such as a record's generated"
                + " PrintMembers and ToString, are not read. A boxed value stored first and formatted later,"
                + " a serializer that writes names, and a name read through a generic helper are not seen;"
                + " whether the text reaches a client stays with review.");

    /// <summary>How a call spells an enum by its member name, or <c>null</c>; <paramref name="spelled"/> is the enum.</summary>
    private static string? EnumNameUse(MethodBase call, Type? boxedEnum, Func<Type?, bool> innerEnum, out Type? spelled)
    {
        spelled = null;
        if (call is MethodInfo { IsGenericMethod: true } generic)
        {
            var argument = generic.GetGenericArguments().FirstOrDefault(innerEnum);
            if (argument is not null && (call.DeclaringType == typeof(System.Enum) || call.Name == "AppendFormatted"))
            {
                spelled = argument;
                return call.Name == "AppendFormatted" ? "interpolates" : $"calls Enum.{call.Name} on";
            }
        }

        if (boxedEnum is null)
        {
            return null;
        }

        spelled = boxedEnum;
        if (call.Name == "ToString")
        {
            return "calls ToString on";
        }

        var owner = call.DeclaringType;
        if (owner == typeof(string) && call.Name is "Concat" or "Format" or "Join"
            || owner?.FullName == "System.Text.StringBuilder" && call.Name is "Append" or "AppendFormat"
            || owner?.FullName is "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler")
        {
            return $"formats into text ({owner!.Name}.{call.Name})";
        }

        return null;
    }

    private static bool IsGetHandler(MethodInfo method, FrameworkTypes roles, bool byPrefix) =>
        (!method.IsSpecialName && method.GetCustomAttributesData().Any(a => roles.GetMappingAttributeTypes.Any(r => DcaMarkers.IsAssignableToByName(a.AttributeType, r))))
        || (byPrefix && method.IsPublic && !method.IsStatic && roles.GetHandlerNamePrefixes.Any(p => p.Length > 0 && method.Name.StartsWith(p, StringComparison.Ordinal)));

    /// <summary>The command type an input-port call takes, or <c>null</c> when the call is no such call.</summary>
    private static string? CommandTakenBy(MethodBase call, DcaMarkers markers)
    {
        if (call.DeclaringType is null || !DcaMarkers.IsAssignableToByName(call.DeclaringType, markers.InputPort))
        {
            return null;
        }

        return call.GetParameters()
            .Select(p => p.ParameterType.Name.Split('`')[0])
            .FirstOrDefault(name => name.EndsWith("Command", StringComparison.Ordinal));
    }

    /// <summary>A use case implementation: a class (never an interface) behind an <see cref="IInputPort"/>.</summary>
    private static bool IsUseCaseImplementation(DcaArchitecture arch, IType type) =>
        type is Class && !type.IsGenericParameter && IsAssignableTo(arch, type, arch.Layout.Markers.InputPort);

    private static bool InNamespace(IType type, string pattern) =>
        type.Namespace is not null && Regex.IsMatch(type.Namespace.FullName, pattern);

    private static bool IsAssignableTo(DcaArchitecture arch, IType type, string marker)
    {
        var markerName = marker;
        if (type.FullName == markerName || type.ImplementsInterface(markerName) || type.IsAssignableTo(markerName))
        {
            return true;
        }

        var runtime = arch.RuntimeType(type);
        return runtime is not null && DcaMarkers.IsAssignableToByName(runtime, marker);
    }
}
