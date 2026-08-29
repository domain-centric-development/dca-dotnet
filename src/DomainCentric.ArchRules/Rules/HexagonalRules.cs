using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace DomainCentric.ArchRules.Rules;

/// <summary>
/// Hexagonal Architecture (Ports and Adapters) rules: separation between ports and adapters,
/// incoming adapters drive the application, outgoing adapters implement outbound ports, adapters
/// never talk to each other directly, incoming adapters stay inside their own bounded context.
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
        }.AsReadOnly();
    }

    public string Name => "hexagonal";

    public IReadOnlyList<IDcaRule> Rules { get; }

    /// <summary>The layout this rule set was built for.</summary>
    public DcaLayout Layout { get; }

    /// <summary>Pattern of event consumers, which may depend on other contexts' integration events.</summary>
    private string EventConsumerPattern() =>
        DcaLayout.Below($"{Layout.RootNamespace}.{DcaLayout.Segment}.{Layout.AdapterSegment}.{Layout.IncomingSegment}.Event");

    /// <summary>Regular expression matching any of the given namespace patterns.</summary>
    internal static string AnyOf(IEnumerable<string> patterns) =>
        string.Join("|", patterns.Select(p => $"(?:{p})"));

    /// <summary>
    /// Whether a class is a controller: it derives from the configured controller/page-model base, carries
    /// the API-controller attribute, or its name ends with <c>Controller</c> or the layout's controller suffix.
    /// </summary>
    internal static bool IsController(Class type, DcaLayout layout) =>
        type.Name.EndsWith("Controller", StringComparison.Ordinal)
        || type.Name.EndsWith(layout.RestControllerSuffix, StringComparison.Ordinal)
        || type.IsAssignableTo(layout.FrameworkTypes.ControllerBase)
        || type.IsAssignableTo(layout.FrameworkTypes.PageModelBase)
        || type.Attributes.Any(a => a.FullName == layout.FrameworkTypes.ApiControllerAttribute);

    public IDcaRule DomainMustNotAccessAdapters() =>
        DcaRule.Of(
            "DCA-HEX-001",
            "Classes from the domain should not access port adapters",
            "Domain should not depend on adapters (ports and adapters pattern)",
            arch => Types().That().ResideInNamespaceMatching(Layout.DomainModelPattern)
                .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(Layout.AdapterPattern));

    public IDcaRule ApplicationMustNotAccessAdapters() =>
        DcaRule.Of(
            "DCA-HEX-002",
            "Application Services should not access port adapters",
            "Application services should only depend on domain and outbound ports, not adapters",
            arch => Types().That().ResideInNamespaceMatching(Layout.ApplicationPattern)
                .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(Layout.AdapterPattern));

    public IDcaRule ControllersMustNotAccessRepositories() =>
        DcaRule.Of(
            "DCA-HEX-003",
            "Controllers and Resources must never access repositories directly",
            "Controllers must go through use cases (input ports), never directly to repositories",
            arch => Classes().That().FollowCustomPredicate(c => IsController(c, Layout), "are controllers")
                .Should().NotDependOnAnyTypesThat()
                .FollowCustomPredicate(t => t.IsAssignableTo(typeof(IRepository).FullName!), "are repositories"));

    public IDcaRule IncomingAdaptersMustNotUseInfrastructureImplementations() =>
        DcaRule.Of(
            "DCA-HEX-004",
            "Incoming Adapters must only use outbound ports (not infrastructure implementations)",
            "Incoming adapters should only use outbound ports declared as interfaces (Ports.Out), not"
                + " infrastructure implementation details",
            arch => Types().That().ResideInNamespaceMatching(Layout.IncomingAdapterPattern)
                .Should().NotDependOnAnyTypesThat()
                .FollowCustomPredicate(arch.IsInfrastructureImplementation, "are infrastructure implementations"));

    public IDcaRule OutgoingAdaptersMustNotUseInfrastructureImplementations() =>
        DcaRule.Of(
            "DCA-HEX-005",
            "Outgoing Adapters must only use outbound ports (not infrastructure implementations)",
            "Outgoing adapters should only use outbound ports declared as interfaces (Ports.Out), not"
                + " infrastructure implementation details",
            arch => Types().That().ResideInNamespaceMatching(Layout.OutgoingAdapterPattern)
                .Should().NotDependOnAnyTypesThat()
                .FollowCustomPredicate(arch.IsInfrastructureImplementation, "are infrastructure implementations"));

    public IDcaRule AdaptersMustNotCommunicateDirectly() =>
        DcaRule.Of(
            "DCA-HEX-006",
            "Port adapters (incoming and outgoing) must not communicate directly with each other"
                + " within the same context",
            "Port adapters should communicate through application services, not directly (event"
                + " consumers are the exception)",
            arch => Types().That().ResideInNamespaceMatching(Layout.IncomingAdapterPattern)
                .And().DoNotResideInNamespaceMatching(EventConsumerPattern())
                .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(Layout.OutgoingAdapterPattern));

    public IDcaRule IncomingAdaptersStayInOwnContext() =>
        DcaRule.Check(
            "DCA-HEX-007",
            "Incoming adapters must only access their own bounded context (except event consumers and"
                + " Open Host Services)",
            "Incoming adapters must only orchestrate use cases from their own bounded context - use"
                + " domain events for cross-context integration",
            arch =>
            {
                foreach (var (contextNamespace, context) in arch.BoundedContexts.Select(e => (e.Key, e.Value)))
                {
                    var otherContexts = arch.BoundedContextPatternsExcluding(contextNamespace);
                    if (otherContexts.Length == 0)
                    {
                        continue;
                    }

                    var rule = Types().That().ResideInNamespaceMatching(Layout.IncomingAdapterPatternOf(contextNamespace))
                        .And().DoNotResideInNamespaceMatching(EventConsumerPattern())
                        .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(AnyOf(otherContexts));
                    DcaRule.Evaluate(
                        rule,
                        arch,
                        $"Incoming adapters in '{context.Name}' must only access their own bounded context",
                        "Incoming adapters in '" + context.Name + "' must only orchestrate use cases from their own"
                            + " bounded context - use domain events for cross-context integration");
                }
            });

    public IDcaRule RepositoryClassesResideInOutgoingAdapter() =>
        DcaRule.Of(
            "DCA-HEX-008",
            "Classes named *Repository must reside in the outgoing adapter namespace",
            "Repository implementations are secondary adapters (outgoing ports)",
            arch => Classes().That().HaveNameEndingWith("Repository")
                .Should().ResideInNamespaceMatching(Layout.OutgoingAdapterPattern));

    public IDcaRule SharedOutputPortsExtendOutputPort() =>
        DcaRule.Of(
            "DCA-HEX-009",
            "Output Ports in Application.Shared must extend IOutputPort",
            "Top-level interfaces in Application.Shared are output ports and must extend IOutputPort to"
                + " be part of the port hierarchy. Nested interfaces (e.g. IIdentityProvider.Identity)"
                + " are part of their enclosing port's contract, not ports themselves",
            arch => Interfaces().That().ResideInNamespaceMatching(Layout.SharedOutputPortPattern)
                .And().FollowCustomPredicate(i => !i.IsNested, "are top-level interfaces")
                .Should().BeAssignableTo(typeof(IOutputPort)));

    public IDcaRule OutputPortsMustNotResideInDomain() =>
        DcaRule.Of(
            "DCA-HEX-010",
            "Output ports must not reside in the domain layer",
            "output ports (IRepository, IStore, IOutputPort) are an application-layer concern and must live"
                + " in Application/Shared/, not Domain/",
            arch => Interfaces().That().AreAssignableTo(typeof(IOutputPort))
                .Should().NotResideInNamespaceMatching(Layout.DomainPattern));
}
