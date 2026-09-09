using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DomainCentric.ArchRules;

/// <summary>
/// Describes how a Domain-Centric Architecture code base is laid out in namespaces, so that the DCA
/// rules can be applied to any project regardless of its root namespace or naming conventions.
/// </summary>
/// <remarks>
/// <para>Obtain the DCA defaults with <see cref="ForRootNamespace"/> and override individual settings
/// through the fluent <c>With*</c> methods:</para>
/// <code>
/// var layout = DcaLayout.ForRootNamespace("Acme.Shop")
///     .WithIncomingSegment("In")
///     .WithOutgoingSegment("Out");
/// </code>
/// <para>Every <c>*Pattern</c> member returns a .NET regular expression over full namespace names,
/// ready for ArchUnitNET's <c>ResideInNamespaceMatching(pattern)</c>. Layout segments are matched
/// case-sensitively, exactly as written.</para>
/// <para>The layout assumes one root namespace per application. A namespace at any depth below it
/// that carries a <c>[BoundedContext]</c> marker class is a bounded context (<c>Acme.Shop.Cart</c>,
/// <c>Acme.Shop.Sales.Order</c>, or the root itself for a single-context application); the one carrying
/// <c>[SharedKernel]</c> is the shared kernel. Bounded contexts may live in one assembly or in one
/// assembly each — the rules work on namespaces, so both layouts are supported.</para>
/// <para><b>Wildcard patterns versus discovered modules.</b> The parameterless <c>*Pattern</c>
/// properties (<see cref="DomainPattern"/> and siblings) build <c>Root.[^.]+.Domain</c>, where the
/// wildcard is exactly one segment — they only ever match a module that is a direct child of the root
/// namespace. They are kept for tooling that needs a pattern without a loaded type graph. <b>The rules
/// do not use them.</b> Every rule selects through <see cref="DcaArchitecture"/>'s discovery accessors
/// (<c>AllDomainPatterns()</c>, <c>ContextDomainPatterns()</c>, …), built from the module roots and
/// declared contexts actually present, and so works at any depth. The wildcard is deliberately not
/// loosened to <c>.*</c>: that would also match any namespace merely <em>named</em> <c>Domain</c> further
/// down, such as an outgoing adapter mapping to a foreign model.</para>
/// </remarks>
public sealed class DcaLayout
{
    /// <summary>Root namespace of the DomainCentric.BuildingBlocks package.</summary>
    public const string BuildingBlocksNamespace = "DomainCentric.BuildingBlocks";

    public const string BuildingBlocksTacticalNamespace = "DomainCentric.BuildingBlocks.Ddd.Tactical";
    public const string BuildingBlocksStrategicNamespace = "DomainCentric.BuildingBlocks.Ddd.Strategic";
    public const string BuildingBlocksPortsNamespace = "DomainCentric.BuildingBlocks.Hexagonal.Ports";
    public const string BuildingBlocksPortsInNamespace = "DomainCentric.BuildingBlocks.Hexagonal.Ports.In";
    public const string BuildingBlocksPortsOutNamespace = "DomainCentric.BuildingBlocks.Hexagonal.Ports.Out";

    /// <summary>Namespace prefixes the domain layer may use by default: the BCL and the building blocks.</summary>
    private static readonly IReadOnlyList<string> DefaultThirdPartyAllowedInDomain =
        new[] { "System", "Microsoft.Extensions.Logging.Abstractions", BuildingBlocksNamespace };

    private DcaLayout(
        string rootNamespace,
        string sharedKernelSegment,
        string domainSegment,
        string applicationSegment,
        string adapterSegment,
        string incomingSegment,
        string outgoingSegment,
        string infrastructureSegment,
        string apiSegment,
        string eventsSegment,
        string useCaseSuffix,
        string controllerSuffix,
        string restControllerSuffix,
        IReadOnlyList<string> thirdPartyNamespacesAllowedInDomain,
        FrameworkTypes frameworkTypes)
    {
        RootNamespace = RequireSegment(rootNamespace, nameof(rootNamespace));
        SharedKernelSegment = RequireSegment(sharedKernelSegment, nameof(sharedKernelSegment));
        DomainSegment = RequireSegment(domainSegment, nameof(domainSegment));
        ApplicationSegment = RequireSegment(applicationSegment, nameof(applicationSegment));
        AdapterSegment = RequireSegment(adapterSegment, nameof(adapterSegment));
        IncomingSegment = RequireSegment(incomingSegment, nameof(incomingSegment));
        OutgoingSegment = RequireSegment(outgoingSegment, nameof(outgoingSegment));
        InfrastructureSegment = RequireSegment(infrastructureSegment, nameof(infrastructureSegment));
        ApiSegment = RequireSegment(apiSegment, nameof(apiSegment));
        EventsSegment = RequireSegment(eventsSegment, nameof(eventsSegment));
        if (ApiSegment == EventsSegment)
        {
            throw new ArgumentException($"apiSegment and eventsSegment must differ, both are '{ApiSegment}'", nameof(eventsSegment));
        }

        UseCaseSuffix = RequireSegment(useCaseSuffix, nameof(useCaseSuffix));
        ControllerSuffix = RequireSegment(controllerSuffix, nameof(controllerSuffix));
        RestControllerSuffix = RequireSegment(restControllerSuffix, nameof(restControllerSuffix));
        ThirdPartyNamespacesAllowedInDomain = thirdPartyNamespacesAllowedInDomain.ToArray();
        FrameworkTypes = frameworkTypes ?? throw new ArgumentNullException(nameof(frameworkTypes));
    }

    /// <summary>The DCA default layout for the given root namespace.</summary>
    public static DcaLayout ForRootNamespace(string rootNamespace) =>
        new(
            rootNamespace,
            "SharedKernel",
            "Domain",
            "Application",
            "Adapter",
            "Incoming",
            "Outgoing",
            "Infrastructure",
            "Api",
            "Events",
            "UseCase",
            "Controller",
            "Controller",
            DefaultThirdPartyAllowedInDomain,
            FrameworkTypes.AspNetCore());

    private static string RequireSegment(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} must not be blank", name);
        }

        return value;
    }

    // ---------------------------------------------------------------------------------------------
    // Plain values
    // ---------------------------------------------------------------------------------------------

    public string RootNamespace { get; }
    public string SharedKernelSegment { get; }
    public string DomainSegment { get; }
    public string ApplicationSegment { get; }
    public string AdapterSegment { get; }
    public string IncomingSegment { get; }
    public string OutgoingSegment { get; }
    public string InfrastructureSegment { get; }

    /// <summary>Segment of a module's <em>synchronous</em> published contract (<c>Api</c> by default).</summary>
    public string ApiSegment { get; }

    /// <summary>Segment of a module's <em>asynchronous</em> published contract — its integration events (<c>Events</c> by default).</summary>
    public string EventsSegment { get; }

    /// <summary>
    /// The published segments of a module, <see cref="ApiSegment"/> then <see cref="EventsSegment"/>:
    /// DCA's in-process contract convention — namespace names, not framework attributes — and the only
    /// part of a module another module's adapters may depend on.
    /// </summary>
    public IReadOnlyList<string> PublishedSegments => new[] { ApiSegment, EventsSegment };

    /// <summary>Suffix of use-case implementations, e.g. <c>UseCase</c> or <c>ApplicationService</c>.</summary>
    public string UseCaseSuffix { get; }

    /// <summary>Suffix of MVC (server-rendered) controllers and page models, e.g. <c>Controller</c> (default) or <c>Page</c>. Read by the naming rule for MVC controllers and by the rule that keeps controllers away from repositories.</summary>
    public string ControllerSuffix { get; }

    /// <summary>Suffix of REST/API controllers ([ApiController]), e.g. <c>Controller</c> (default) or <c>Resource</c>.</summary>
    public string RestControllerSuffix { get; }

    /// <summary>
    /// Namespace prefixes the domain layer may depend on besides its own code (exact namespace or any
    /// sub-namespace). Default: <c>System</c>, <c>Microsoft.Extensions.Logging.Abstractions</c> and the
    /// building blocks.
    /// </summary>
    public IReadOnlyList<string> ThirdPartyNamespacesAllowedInDomain { get; }

    /// <summary>
    /// The framework types the rules look for, by role — <see cref="FrameworkTypes.AspNetCore"/> (default),
    /// <see cref="FrameworkTypes.None"/>, or an adjusted preset. The preset in use is part of <see cref="ToString"/>.
    /// </summary>
    public FrameworkTypes FrameworkTypes { get; }

    // ---------------------------------------------------------------------------------------------
    // Fluent overrides
    // ---------------------------------------------------------------------------------------------

    public DcaLayout WithSharedKernelSegment(string value) => Copy(sharedKernel: value);

    public DcaLayout WithDomainSegment(string value) => Copy(domain: value);

    public DcaLayout WithApplicationSegment(string value) => Copy(application: value);

    public DcaLayout WithAdapterSegment(string value) => Copy(adapter: value);

    /// <summary>Name of the incoming (driving/primary) adapter namespace segment — <c>In</c> in some projects.</summary>
    public DcaLayout WithIncomingSegment(string value) => Copy(incoming: value);

    /// <summary>Name of the outgoing (driven/secondary) adapter namespace segment — <c>Out</c> in some projects.</summary>
    public DcaLayout WithOutgoingSegment(string value) => Copy(outgoing: value);

    public DcaLayout WithInfrastructureSegment(string value) => Copy(infrastructure: value);

    /// <summary>
    /// Segment of a module's synchronous published contract, e.g. <c>Api</c> (default) or <c>Contract</c>.
    /// Together with <see cref="WithEventsSegment"/> it is the only part of a module another module's
    /// adapters may depend on; the context-map rules and renderer use the same name for the channel.
    /// </summary>
    public DcaLayout WithApiSegment(string value) => Copy(api: value);

    /// <summary>Segment of a module's asynchronous published contract — its integration events — e.g. <c>Events</c> (default).</summary>
    public DcaLayout WithEventsSegment(string value) => Copy(events: value);

    public DcaLayout WithUseCaseSuffix(string value) => Copy(useCaseSuffix: value);

    public DcaLayout WithControllerSuffix(string value) => Copy(controllerSuffix: value);

    public DcaLayout WithRestControllerSuffix(string value) => Copy(restControllerSuffix: value);

    /// <summary>Replaces the list of third-party namespace prefixes the domain layer may depend on.</summary>
    public DcaLayout WithThirdPartyNamespacesAllowedInDomain(IEnumerable<string> prefixes) =>
        Copy(thirdParty: prefixes.ToArray());

    /// <summary>Adds third-party namespace prefixes to the domain allow-list.</summary>
    public DcaLayout AllowingInDomain(params string[] prefixes) =>
        Copy(thirdParty: ThirdPartyNamespacesAllowedInDomain.Concat(prefixes).ToArray());

    public DcaLayout WithFrameworkTypes(FrameworkTypes value) => Copy(frameworkTypes: value);

    private DcaLayout Copy(
        string? sharedKernel = null,
        string? domain = null,
        string? application = null,
        string? adapter = null,
        string? incoming = null,
        string? outgoing = null,
        string? infrastructure = null,
        string? api = null,
        string? events = null,
        string? useCaseSuffix = null,
        string? controllerSuffix = null,
        string? restControllerSuffix = null,
        IReadOnlyList<string>? thirdParty = null,
        FrameworkTypes? frameworkTypes = null) =>
        new(
            RootNamespace,
            sharedKernel ?? SharedKernelSegment,
            domain ?? DomainSegment,
            application ?? ApplicationSegment,
            adapter ?? AdapterSegment,
            incoming ?? IncomingSegment,
            outgoing ?? OutgoingSegment,
            infrastructure ?? InfrastructureSegment,
            api ?? ApiSegment,
            events ?? EventsSegment,
            useCaseSuffix ?? UseCaseSuffix,
            controllerSuffix ?? ControllerSuffix,
            restControllerSuffix ?? RestControllerSuffix,
            thirdParty ?? ThirdPartyNamespacesAllowedInDomain,
            frameworkTypes ?? FrameworkTypes);

    // ---------------------------------------------------------------------------------------------
    // Derived namespace names and patterns
    // ---------------------------------------------------------------------------------------------

    /// <summary><c>Root.SharedKernel</c> (plain namespace, no pattern).</summary>
    public string SharedKernelNamespace => $"{RootNamespace}.{SharedKernelSegment}";

    /// <summary><c>Root.Infrastructure</c> (plain namespace, no pattern).</summary>
    public string InfrastructureNamespace => $"{RootNamespace}.{InfrastructureSegment}";

    /// <summary>Pattern for <c>Root.SharedKernel</c> and everything below.</summary>
    public string SharedKernelPattern => Below(SharedKernelNamespace);

    /// <summary>Pattern for <c>Root.SharedKernel.Domain</c> and below.</summary>
    public string SharedKernelDomainPattern => Below($"{SharedKernelNamespace}.{DomainSegment}");

    /// <summary>Pattern for <c>Root.SharedKernel.Domain.Model</c> and below.</summary>
    public string SharedKernelDomainModelPattern => Below($"{SharedKernelNamespace}.{DomainSegment}.Model");

    /// <summary>Pattern for <c>Root.Infrastructure</c> and below.</summary>
    public string InfrastructurePattern => Below(InfrastructureNamespace);

    /// <summary>Pattern for <c>Root.*.Domain</c> and below — the domain layer of every direct child namespace (context).</summary>
    public string DomainPattern => Below($"{RootNamespace}.{Segment}.{DomainSegment}");

    /// <summary>Pattern for <c>Root.*.Domain.Model</c> and below.</summary>
    public string DomainModelPattern => Below($"{RootNamespace}.{Segment}.{DomainSegment}.Model");

    /// <summary>Pattern for <c>Root.*.Application</c> and below.</summary>
    public string ApplicationPattern => Below($"{RootNamespace}.{Segment}.{ApplicationSegment}");

    /// <summary>Pattern for <c>Root.*.Application.Shared</c> and below — output ports shared by the use cases of one context.</summary>
    public string SharedOutputPortPattern => Below($"{RootNamespace}.{Segment}.{ApplicationSegment}.Shared");

    /// <summary>Pattern for <c>Root.*.Adapter</c> and below.</summary>
    public string AdapterPattern => Below($"{RootNamespace}.{Segment}.{AdapterSegment}");

    /// <summary>Pattern for <c>Root.*.Adapter.Incoming</c> and below.</summary>
    public string IncomingAdapterPattern => Below($"{RootNamespace}.{Segment}.{AdapterSegment}.{IncomingSegment}");

    /// <summary>Pattern for <c>Root.*.Adapter.Outgoing</c> and below.</summary>
    public string OutgoingAdapterPattern => Below($"{RootNamespace}.{Segment}.{AdapterSegment}.{OutgoingSegment}");

    // Per-context variants — contextNamespace is a plain namespace such as Acme.Shop.Cart.

    public string DomainPatternOf(string contextNamespace) => Below($"{contextNamespace}.{DomainSegment}");

    public string DomainModelPatternOf(string contextNamespace) => Below($"{contextNamespace}.{DomainSegment}.Model");

    public string ApplicationPatternOf(string contextNamespace) => Below($"{contextNamespace}.{ApplicationSegment}");

    public string SharedOutputPortPatternOf(string contextNamespace) => Below($"{contextNamespace}.{ApplicationSegment}.Shared");

    public string AdapterPatternOf(string contextNamespace) => Below($"{contextNamespace}.{AdapterSegment}");

    public string IncomingAdapterPatternOf(string contextNamespace) => Below($"{contextNamespace}.{AdapterSegment}.{IncomingSegment}");

    public string OutgoingAdapterPatternOf(string contextNamespace) => Below($"{contextNamespace}.{AdapterSegment}.{OutgoingSegment}");

    // ---------------------------------------------------------------------------------------------
    // Pattern helpers
    // ---------------------------------------------------------------------------------------------

    /// <summary>Regex fragment matching exactly one namespace segment (ArchUnit's <c>*</c>).</summary>
    public const string Segment = @"[^.]+";

    /// <summary>
    /// Regex matching <paramref name="ns"/> itself and every namespace below it (ArchUnit's <c>ns..</c>).
    /// Literal parts are escaped; <see cref="Segment"/> wildcards are kept.
    /// </summary>
    public static string Below(string ns) => $"^{EscapeKeepingSegments(ns)}(\\..*)?$";

    /// <summary>
    /// Alternation of complete namespace patterns (each already anchored). An empty list yields a
    /// pattern that matches nothing — never an empty string, which would match everything.
    /// </summary>
    public static string AnyOf(IEnumerable<string> patterns)
    {
        var list = patterns.ToList();
        return list.Count == 0 ? "(?!)" : string.Join("|", list.Select(p => $"(?:{p})"));
    }

    /// <summary>ArchUnit's <c>..A.B..</c>: the dotted path appears anywhere on segment boundaries.</summary>
    public static string AnySegmentPath(string dottedPath) =>
        @"^(?:.*\.)?" + Regex.Escape(dottedPath) + @"(?:\..*)?$";

    /// <summary>Regex matching exactly <paramref name="ns"/> (no sub-namespaces).</summary>
    public static string Exactly(string ns) => $"^{EscapeKeepingSegments(ns)}$";

    /// <summary>Whether <paramref name="fullNamespace"/> is <paramref name="prefix"/> or lies below it.</summary>
    public static bool IsBelow(string fullNamespace, string prefix) =>
        fullNamespace == prefix || fullNamespace.StartsWith(prefix + ".", StringComparison.Ordinal);

    private static string EscapeKeepingSegments(string ns) =>
        string.Join(Segment, ns.Split(new[] { Segment }, StringSplitOptions.None).Select(Regex.Escape));

    public override string ToString() => $"DcaLayout[{RootNamespace}, frameworkTypes={FrameworkTypes}]";
}
