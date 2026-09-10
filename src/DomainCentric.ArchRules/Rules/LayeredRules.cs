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
        DcaRule.Informational(
            "DCA-LAY-001",
            "Diagnostic: The rules of the Layered Architecture should be followed",
            "Traditional layering (application accessed only by incoming adapters) contradicts Ports"
                + " and Adapters, where outgoing adapters implement application-level output ports;"
                + " the hexagonal rules cover the intended dependency direction",
            arch => { })
            .Selecting(
                "Informational - selects nothing. A classic layered-architecture definition (adapter "
                + "layer accesses application, application accesses domain, domain accesses nothing) is not "
                + "built, because in Ports and Adapters outgoing adapters implement application-level "
                + "output ports.")
            .Checking(
                "Informational - selects nothing and never fails; it carries doctrine only. The "
                + "dependency direction is enforced by the hexagonal rules.");

    public IDcaRule DomainMustNotDependOnInfrastructure() =>
        DcaRule.Of(
            "DCA-LAY-002",
            "Domain must not have dependencies on Infrastructure",
            "Domain should not depend on infrastructure concerns (Dependency Inversion Principle)",
            // The global infrastructure namespace and every isolated module's own.
            arch => Types().That().ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllDomainPatterns()))
                .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllInfrastructurePatterns())))
            .Selecting(
                "Types in <module>.Domain of every module root, the shared kernel's domain included.")
            .Checking(
                "No dependency on a type in the global infrastructure namespace (<root>.Infrastructure or "
                + "below) or in any isolated module's own infrastructure namespace (<module>.Infrastructure "
                + "or below). The shared kernel's infrastructure namespace is not in that list. A module "
                + "without a domain layer selects nothing and passes.");

    public IDcaRule ApplicationMustNotUseInfrastructureImplementations() =>
        DcaRule.Of(
            "DCA-LAY-003",
            "Application Services must only use outbound ports (not infrastructure implementations)",
            "Application services should only use outbound ports declared as interfaces (Ports.Out), not"
                + " infrastructure implementation details",
            arch => Types().That().ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllApplicationPatterns()))
                .Should().NotDependOnAnyTypesThat()
                .FollowCustomPredicate(arch.IsInfrastructureImplementation, "are infrastructure implementations"))
            .Selecting(
                "Types in <module>.Application of every module root.")
            .Checking(
                "No dependency on a type residing in the global infrastructure namespace or in any "
                + "isolated module's own infrastructure namespace, sub-namespaces included, with an exact "
                + "segment boundary. Dependencies on outgoing adapters are not checked here - only "
                + "infrastructure namespaces count.");

    /// <summary>
    /// Every type that uses the configured transaction type (<c>TransactionScope</c>) must reside in the
    /// application layer or in an outgoing adapter — the .NET reading of the Java rule's "members carrying
    /// the configured transactional annotation".
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
                // Structural, over every module root: the application layer and the outgoing adapters
                // (which implement the transaction boundary) of any module, at any depth.
                var allowed = new Regex(
                    DcaLayout.AnyOf(arch.AllApplicationPatterns().Concat(arch.AllOutgoingAdapterPatterns())));
                var transactionType = Layout.FrameworkTypes.TransactionScope;
                var violations = arch.Types
                    .Where(t => FrameworkTypes.IsSet(transactionType) && t.Dependencies.Any(d => d.Target.FullName == transactionType))
                    .Where(t => t.Namespace is null || !allowed.IsMatch(t.Namespace.FullName))
                    .Select(t => $"{t.FullName} uses {transactionType} outside the application layer")
                    .ToList();
                DcaRule.Fail($"Transaction boundaries belong to the application layer\nbecause {rationale}", violations);
            })
            .Selecting(
                "Types under scan that have any dependency on the configured transaction type (by default "
                + "System.Transactions.TransactionScope) - a field, a local, a method call or a using block "
                + "all count. With no transaction type configured nothing is selected.")
            .Checking(
                "Each resides in an application namespace of some module root (<module>.Application or "
                + "below) or in an outgoing adapter namespace of some module root "
                + "(<module>.Adapter.Outgoing or below). A use in a domain, incoming-adapter or "
                + "infrastructure namespace is reported; all findings are collected into one violation. The "
                + "check is per type, not per method, and other transaction APIs (a DbContext transaction, "
                + "TransactionScope subclasses) are not looked for.");
    }

    /// <summary>
    /// Checked by reflection on the building-blocks assembly: the outbound-port markers are not part of the
    /// analysed application namespace, but they are what every application-layer port derives from.
    /// </summary>
    public IDcaRule OutputPortMarkersMustBeInterfaces()
    {
        const string title = "No implementation is placed into the building-blocks output-port package";
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
                var violations = arch.Architecture.Types.Select(arch.RuntimeType).Where(t => t is not null).Cast<Type>()
                    .Where(t => t.Namespace == DcaLayout.BuildingBlocksPortsOutNamespace && !t.IsInterface)
                    // closures and async state machines of default interface methods are not port types
                    .Where(t => !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false) && !t.Name.Contains('<'))
                    .Select(t => $"{t.FullName} is not an interface")
                    .ToList();
                DcaRule.Fail($"{title}\nbecause {rationale}", violations);
            })
            .Selecting(
                "Types declared in the building-blocks namespace "
                + "DomainCentric.BuildingBlocks.Hexagonal.Ports.Out, read from types imported in the architecture, including consumer assemblies. Compiler-generated "
                + "types (closures, async state machines of default interface methods) are not selected.")
            .Checking(
                "Each is an interface. The project's own output ports in Application.Shared are not "
                + "selected; the rule passes when no non-interface type is found.");
    }
}
