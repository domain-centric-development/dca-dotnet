using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace DomainCentric.ArchRules.Rules;

/// <summary>
/// Layered Architecture rules: dependencies point inward, the domain knows no infrastructure, the
/// application layer talks to outbound ports only, and transactions are an application concern.
/// </summary>
public sealed class LayeredRules : IDcaRuleSet
{
    /// <summary>Java rules of this set that have no .NET reading (none).</summary>
    public static readonly IReadOnlyDictionary<string, string> NotApplicable = new Dictionary<string, string>();

    public LayeredRules(DcaLayout layout)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Rules = new List<IDcaRule>
        {
            LayeredArchitectureDiagnostic(),
            DomainMustNotDependOnInfrastructure(),
            ApplicationMustNotUseInfrastructureImplementations(),
            TransactionBoundariesBelongToApplicationLayer(),
            OutputPortMarkersMustBeInterfaces(),
        }.AsReadOnly();
    }

    public string Name => "layered";

    public IReadOnlyList<IDcaRule> Rules { get; }

    /// <summary>The layout this rule set was built for.</summary>
    public DcaLayout Layout { get; }

    /// <summary>
    /// Documentation-only: a classic layered-architecture check does not fit Ports and Adapters, where
    /// both adapter types depend on the application layer. The hexagonal dependency rules are enforced by
    /// <see cref="HexagonalRules"/> instead. This rule never fails.
    /// </summary>
    public IDcaRule LayeredArchitectureDiagnostic() =>
        DcaRule.Check(
            "DCA-LAY-001",
            "Diagnostic: The rules of the Layered Architecture should be followed",
            "Traditional layering (application accessed only by incoming adapters) contradicts Ports"
                + " and Adapters, where outgoing adapters implement application-level output ports;"
                + " the hexagonal rules cover the intended dependency direction",
            arch => { });

    public IDcaRule DomainMustNotDependOnInfrastructure() =>
        DcaRule.Of(
            "DCA-LAY-002",
            "Domain must not have dependencies on Infrastructure",
            "Domain should not depend on infrastructure concerns (Dependency Inversion Principle)",
            arch => Types().That().ResideInNamespaceMatching(Layout.DomainPattern)
                .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(Layout.InfrastructurePattern));

    public IDcaRule ApplicationMustNotUseInfrastructureImplementations() =>
        DcaRule.Of(
            "DCA-LAY-003",
            "Application Services must only use outbound ports (not infrastructure implementations)",
            "Application services should only use outbound ports declared as interfaces (Ports.Out), not"
                + " infrastructure implementation details",
            arch => Types().That().ResideInNamespaceMatching(Layout.ApplicationPattern)
                .Should().NotDependOnAnyTypesThat()
                .FollowCustomPredicate(arch.IsInfrastructureImplementation, "are infrastructure implementations"));

    /// <summary>
    /// Every type that uses the configured transaction type (<c>TransactionScope</c>) must reside in the
    /// application layer or in an outgoing adapter — the .NET reading of "methods/classes annotated
    /// <c>@Transactional</c>".
    /// </summary>
    public IDcaRule TransactionBoundariesBelongToApplicationLayer()
    {
        const string rationale =
            "Transactions are an application-layer concern - domain and incoming adapters must not manage them";
        return DcaRule.Check(
            "DCA-LAY-004",
            "Transaction boundaries belong to the application layer",
            rationale,
            arch =>
            {
                var allowed = new Regex(
                    HexagonalRules.AnyOf(new[]
                    {
                        Layout.ApplicationPattern,
                        DcaLayout.Below($"{Layout.RootNamespace}.{DcaLayout.Segment}.{Layout.AdapterSegment}.{Layout.OutgoingSegment}"),
                    }));
                var transactionType = Layout.FrameworkTypes.TransactionScope;
                var violations = arch.Types
                    .Where(t => t.Dependencies.Any(d => d.Target.FullName == transactionType))
                    .Where(t => t.Namespace is null || !allowed.IsMatch(t.Namespace.FullName))
                    .Select(t => $"{t.FullName} uses {transactionType} outside the application layer")
                    .ToList();
                DcaRule.Fail($"Transaction boundaries belong to the application layer\nbecause {rationale}", violations);
            });
    }

    /// <summary>
    /// Checked by reflection on the building-blocks assembly: the outbound-port markers are not part of the
    /// analysed application namespace, but they are what every application-layer port derives from.
    /// </summary>
    public IDcaRule OutputPortMarkersMustBeInterfaces()
    {
        const string title = "The shared kernel's output-port markers must all be interfaces";
        const string rationale =
            "Ports.Out contains outbound port interfaces (IRepository, IOutputPort, IDomainEventPublisher)"
                + " shared across all bounded contexts. These must be interfaces to ensure the"
                + " application layer remains framework-independent and follows the Dependency"
                + " Inversion Principle. Implementations belong in infrastructure or adapter namespaces.";
        return DcaRule.Check(
            "DCA-LAY-005",
            title,
            rationale,
            arch =>
            {
                var violations = typeof(IOutputPort).Assembly.GetTypes()
                    .Where(t => t.Namespace == DcaLayout.BuildingBlocksPortsOutNamespace && !t.IsInterface)
                    .Select(t => $"{t.FullName} is not an interface")
                    .ToList();
                DcaRule.Fail($"{title}\nbecause {rationale}", violations);
            });
    }
}
