using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace DomainCentric.ArchRules.Rules;

/// <summary>
/// Naming conventions: consistent names for use cases, input ports, repositories, controllers, DTOs,
/// converters and view models, and ubiquitous-language names in the domain.
/// </summary>
public sealed class NamingRules : IDcaRuleSet
{
    /// <summary>Java rules of this set that have no .NET counterpart (id → reason).</summary>
    public static readonly IReadOnlyDictionary<string, string> NotApplicable = new Dictionary<string, string>
    {
        ["DCA-NAM-002"] = ".NET has no @Service stereotype — use cases are registered in the DI container by code, there is no attribute to check",
    };

    public NamingRules(DcaLayout layout)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Rules = new IDcaRule[]
        {
            InputPortImplementationsEndWithUseCaseSuffix(layout),
            InputPortInterfacesEndWithInputPort(layout),
            RepositoryInterfacesEndWithRepository(layout),
            ControllersEndWithController(layout),
            RestControllersEndWithRestControllerSuffix(layout),
            DtosResideInAdapterLayer(layout),
            ConvertersResideInAdapterLayer(layout),
            NoTechnicalBucketPackages(layout),
            NoTechnicalSuffixesInDomain(layout),
            ViewModelsResideInIncomingWebAdapters(layout),
        };
    }

    public string Name => "naming";

    public IReadOnlyList<IDcaRule> Rules { get; }

    /// <summary>The layout this rule set was built for.</summary>
    public DcaLayout Layout { get; }

    public static IDcaRule InputPortImplementationsEndWithUseCaseSuffix(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-NAM-001",
            $"Application layer InputPort implementations must end with '{layout.UseCaseSuffix}'",
            "InputPort implementations (use cases) should follow consistent naming conventions (Hexagonal Architecture)",
            arch =>
            {
                var violations = arch.Classes
                    .Where(c => InNamespace(c, DcaLayout.AnyOf(arch.AllApplicationPatterns()))
                        && c.IsRecord != true
                        && IsAssignableTo(arch, c, typeof(IInputPort))
                        && !c.Name.EndsWith(arch.Layout.UseCaseSuffix, StringComparison.Ordinal))
                    .Select(c => $"{c.FullName} implements an input port but does not end with '{arch.Layout.UseCaseSuffix}'")
                    .ToList();
                DcaRule.Fail(
                    $"Application layer InputPort implementations must end with '{arch.Layout.UseCaseSuffix}'",
                    violations,
                    $"rename the class to *{arch.Layout.UseCaseSuffix}");
            });

    public static IDcaRule InputPortInterfacesEndWithInputPort(DcaLayout layout) =>
        // Matched by marker, not by namespace: DCA places each input port in its own use-case
        // namespace, so there is no single ..Application.Port.In.. namespace to point at.
        // .NET interfaces additionally carry the customary 'I' prefix: IPlaceOrderInputPort.
        DcaRule.Check(
            "DCA-NAM-003",
            "InputPort interfaces must end with 'InputPort'",
            "Input port interfaces should follow consistent naming conventions (Hexagonal Architecture)",
            arch =>
            {
                var violations = arch.Interfaces
                    .Where(i => InNamespace(i, DcaLayout.AnyOf(arch.AllApplicationPatterns()))
                        && IsAssignableTo(arch, i, typeof(IInputPort))
                        && !IsBaseInputPortName(i.Name)
                        && !(i.Name.StartsWith("I", StringComparison.Ordinal) && i.Name.EndsWith("InputPort", StringComparison.Ordinal)))
                    .Select(i => $"{i.FullName} is an input port but is not named I*InputPort")
                    .ToList();
                DcaRule.Fail(
                    "InputPort interfaces must end with 'InputPort'",
                    violations,
                    "rename the interface to I<UseCaseName>InputPort");
            });

    public static IDcaRule RepositoryInterfacesEndWithRepository(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-NAM-004",
            "Repository Interfaces must end with 'Repository'",
            "Repository interfaces should follow consistent naming conventions (DDD pattern)",
            arch =>
            {
                var violations = arch.Interfaces
                    .Where(i => InNamespace(i, DcaLayout.AnyOf(arch.AllApplicationPatterns()))
                        && i.Name.Contains("Repository", StringComparison.Ordinal)
                        && i.Name != "Repository"
                        && i.Name != "IRepository"
                        && !i.Name.EndsWith("Repository", StringComparison.Ordinal))
                    .Select(i => $"{i.FullName} does not end with 'Repository'")
                    .ToList();
                DcaRule.Fail("Repository Interfaces must end with 'Repository'", violations, "rename the interface to I<Aggregate>Repository");
            });

    public static IDcaRule ControllersEndWithController(DcaLayout layout) =>
        // Java: @Controller (MVC). .NET: MVC controllers (derive from ControllerBase without [ApiController])
        // and Razor page models (derive from PageModel).
        DcaRule.Check(
            "DCA-NAM-005",
            "Controller classes must end with 'Controller'",
            "MVC controller and page model classes should follow naming conventions",
            arch =>
            {
                var violations = arch.Classes
                    .Where(c => InNamespace(c, DcaLayout.AnyOf(arch.AllIncomingAdapterPatterns()))
                        && IsMvcController(arch, c)
                        && !c.Name.EndsWith("Controller", StringComparison.Ordinal))
                    .Select(c => $"{c.FullName} is a controller but does not end with 'Controller'")
                    .ToList();
                DcaRule.Fail("Controller classes must end with 'Controller'", violations, "rename the class to *Controller");
            });

    public static IDcaRule RestControllersEndWithRestControllerSuffix(DcaLayout layout) =>
        // Java: @RestController. .NET: classes carrying [ApiController].
        DcaRule.Check(
            "DCA-NAM-006",
            $"REST Controllers must end with '{layout.RestControllerSuffix}' (REST best practice)",
            $"[ApiController] classes should end with '{layout.RestControllerSuffix}' following RESTful naming conventions",
            arch =>
            {
                var violations = arch.Classes
                    .Where(c => InNamespace(c, DcaLayout.AnyOf(arch.AllIncomingAdapterPatterns()))
                        && IsApiController(arch, c)
                        && !c.Name.EndsWith(arch.Layout.RestControllerSuffix, StringComparison.Ordinal))
                    .Select(c => $"{c.FullName} is an API controller but does not end with '{arch.Layout.RestControllerSuffix}'")
                    .ToList();
                DcaRule.Fail(
                    $"REST Controllers must end with '{arch.Layout.RestControllerSuffix}'",
                    violations,
                    $"rename the class to *{arch.Layout.RestControllerSuffix}");
            });

    public static IDcaRule DtosResideInAdapterLayer(DcaLayout layout) =>
        DcaRule.Of(
            "DCA-NAM-007",
            "DTOs must reside in the adapter layer, not in domain or application",
            "DTOs are adapter concerns (presentation or external API) - not in domain or application",
            arch =>
                Types()
                    .That()
                    .HaveNameEndingWith("Dto")
                    .And()
                    .ResideInNamespaceMatching(DcaLayout.Below(layout.RootNamespace))
                    .Should()
                    .ResideInNamespaceMatching(layout.AdapterPattern));

    public static IDcaRule ConvertersResideInAdapterLayer(DcaLayout layout) =>
        DcaRule.Of(
            "DCA-NAM-008",
            "Converters must reside in the adapter layer",
            "Converters/Mappers translate between layers and should be in adapters",
            arch =>
                Types()
                    .That()
                    .HaveNameEndingWith("Converter")
                    .And()
                    .ResideInNamespaceMatching(DcaLayout.Below(layout.RootNamespace))
                    .Should()
                    .ResideInNamespaceMatching(layout.AdapterPattern));

    public static IDcaRule NoTechnicalBucketPackages(DcaLayout layout) =>
        // Top-level structure must scream business capabilities (screaming architecture).
        // Technical buckets like 'Entities' or 'Util' hide the domain and attract unrelated code.
        // DTOs/Converters/ViewModels have their own placement rules.
        DcaRule.Of(
            "DCA-NAM-009",
            "No technical bucket namespaces - namespace by domain concept",
            "Namespaces are named after domain concepts from the ubiquitous language, not technical patterns",
            arch =>
                Types()
                    .That()
                    .ResideInNamespaceMatching(DcaLayout.Below(layout.RootNamespace))
                    .Should()
                    .NotResideInNamespaceMatching(TechnicalBucketPattern));

    public static IDcaRule NoTechnicalSuffixesInDomain(DcaLayout layout) =>
        // Domain concepts carry ubiquitous-language names. 'Manager'/'Helper'/'Util' signal
        // a missing domain concept; 'Impl'/'Implementation' signal naming by pattern instead of by specialty.
        DcaRule.Of(
            "DCA-NAM-010",
            "Domain classes must not use technical suffixes (Manager, Helper, Util, Impl)",
            "Domain names come from the ubiquitous language - name services by their specialty, not by technical role",
            arch =>
                Types()
                    .That()
                    .ResideInNamespaceMatching(layout.DomainPattern)
                    .Should()
                    .NotHaveNameMatching("(Manager|Helper|Utils?|Impl|Implementation)$"));

    public static IDcaRule ViewModelsResideInIncomingWebAdapters(DcaLayout layout)
    {
        var incomingWebPattern = DcaLayout.Below(
            $"{layout.RootNamespace}.{DcaLayout.Segment}.{layout.AdapterSegment}.{layout.IncomingSegment}.Web");
        return DcaRule.Of(
            "DCA-NAM-011",
            "ViewModels must reside in Adapter.Incoming.Web namespaces",
            "ViewModels are presentation concerns and must reside in incoming web adapter namespaces",
            arch =>
                Types()
                    .That()
                    .HaveNameEndingWith("ViewModel")
                    .And()
                    .ResideInNamespaceMatching(DcaLayout.Below(layout.RootNamespace))
                    .Should()
                    .ResideInNamespaceMatching(incomingWebPattern));
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    /// <summary>Namespaces containing a technical bucket segment (any casing): Entities, ValueObjects, Helpers, Util, Utils.</summary>
    private const string TechnicalBucketPattern = @"(?i)(^|\.)(entities|valueobjects|helpers|utils?)(\.|$)";

    private static bool IsBaseInputPortName(string name) =>
        name is "InputPort" or "IInputPort" or "UseCase" or "IUseCase";

    private static bool InNamespace(IType type, string pattern) =>
        type.Namespace is not null && Regex.IsMatch(type.Namespace.FullName, pattern);

    private static bool IsMvcController(DcaArchitecture arch, Class cls) =>
        (DerivesFrom(cls, arch.Layout.FrameworkTypes.ControllerBase) && !IsApiController(arch, cls))
        || DerivesFrom(cls, arch.Layout.FrameworkTypes.PageModelBase);

    private static bool IsApiController(DcaArchitecture arch, Class cls) =>
        cls.AttributeInstances.Any(a => a.Type.FullName == arch.Layout.FrameworkTypes.ApiControllerAttribute);

    private static bool DerivesFrom(Class cls, string baseFullName)
    {
        for (var current = cls.BaseClass; current is not null; current = current.BaseClass)
        {
            if (current.FullName == baseFullName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Assignability via the ArchUnitNET model with a reflection fallback for interface closures the
    /// loader did not resolve (generic base interfaces such as IUseCase&lt;,&gt; → IInputPort).
    /// </summary>
    private static bool IsAssignableTo(DcaArchitecture arch, IType type, Type marker)
    {
        var markerName = marker.FullName!;
        if (type.FullName == markerName || type.ImplementsInterface(markerName) || type.IsAssignableTo(markerName))
        {
            return true;
        }

        var runtime = arch.RuntimeType(type);
        return runtime is not null && marker.IsAssignableFrom(runtime);
    }
}
