using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace DomainCentric.ArchRules.Rules;

/// <summary>
/// Advanced tactical DDD rules: domain events, integration events, domain services, factories and
/// specifications.
/// </summary>
/// <remarks>
/// Reference: Eric Evans, <i>Domain-Driven Design</i> (Domain Events, Services, Factories,
/// Specifications); Vaughn Vernon, <i>Implementing DDD</i> (Domain Events for eventual consistency).
/// </remarks>
public sealed class AdvancedPatternRules : IDcaRuleSet
{
    private const string VersionField = "version";

    private static readonly string[] TimestampTypes =
    {
        typeof(DateTimeOffset).FullName!,
        typeof(DateTime).FullName!,
    };

    /// <summary>Java rules of this set that have no .NET counterpart (id → reason). None.</summary>
    public static readonly IReadOnlyDictionary<string, string> NotApplicable = new Dictionary<string, string>();

    public AdvancedPatternRules(DcaLayout layout)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Rules = new IDcaRule[]
        {
            DomainEventsAreRecords(),
            DomainEventsResideInDomain(),
            DomainEventsAreImmutable(),
            DomainEventsHaveNoFrameworkAttributes(),
            IntegrationEventsAreAnnotatedWithIntegrationEventType(),
            IntegrationEventsHaveNoVersionField(),
            DomainOnlyEventsHaveNoVersionField(),
            DomainEventsHaveTimestampField(),
            DomainServicesResideInDomainService(),
            DomainServicesResideInDomain(),
            DomainServicesHaveNoFrameworkAttributes(),
            DomainServicesAreStateless(),
            FactoriesAreNamedFactory(),
            FactoriesResideInDomain(),
            FactoriesHaveNoFrameworkAttributes(),
            FactoriesAreStateless(),
            SpecificationsResideInDomain(),
            SpecificationsHaveNoFrameworkAttributes(),
        };
    }

    public string Name => "advanced";

    public IReadOnlyList<IDcaRule> Rules { get; }

    /// <summary>The layout this rule set was built for.</summary>
    public DcaLayout Layout { get; }

    // ============================================================================
    // DOMAIN EVENTS PATTERN
    // ============================================================================

    public IDcaRule DomainEventsAreRecords() =>
        DcaRule.Check(
            "DCA-ADV-001",
            "Domain Events must implement IDomainEvent Marker Interface and be records",
            "Domain events should be immutable records implementing IDomainEvent (named in past tense,"
                + " e.g., ProductCreated, CartCleared)",
            arch => DcaRule.Fail(
                "Domain Events must be records (record class or record struct):",
                Violations(
                    arch,
                    t => t is not Interface && IsDomainEvent(t),
                    t => !IsRecordLike(t),
                    t => $"{t.FullName} implements IDomainEvent but is not a record")));

    public IDcaRule DomainEventsResideInDomain() =>
        DcaRule.Of(
            "DCA-ADV-002",
            "Domain Events must reside in domain namespace",
            "Domain events are part of the domain layer (named in past tense)",
            arch => Types()
                .That()
                .AreAssignableTo(typeof(IDomainEvent))
                .And()
                .FollowCustomPredicate(t => t is not Interface, "are not interfaces")
                .Should()
                .ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllDomainPatterns())));

    public IDcaRule DomainEventsAreImmutable() =>
        DcaRule.Check(
            "DCA-ADV-003",
            "Domain Events should be immutable (sealed or records)",
            "Domain events should be immutable (sealed classes or records)",
            arch => DcaRule.Fail(
                "Domain Events that are not records must be sealed:",
                Violations(
                    arch,
                    t => t is Class c && c.IsRecord != true && InDomain(arch, t) && IsDomainEvent(t),
                    t => ((Class)t).IsSealed != true,
                    t => $"{t.FullName} is neither a record nor sealed")));

    public IDcaRule DomainEventsHaveNoFrameworkAttributes() =>
        DcaRule.Check(
            "DCA-ADV-004",
            "Domain Events must not have framework attributes",
            "Domain events must be framework-independent plain objects",
            arch => FailOnFrameworkAttributes(
                arch,
                t => InDomain(arch, t) && IsDomainEvent(t),
                "Domain Events must not carry framework attributes:"));

    public IDcaRule IntegrationEventsAreAnnotatedWithIntegrationEventType() =>
        DcaRule.Check(
            "DCA-ADV-005",
            "Integration Events must be annotated with IntegrationEventType",
            "[IntegrationEventType(name, Version)] is the contract identity of every integration event"
                + " — the serializer keys (name, version) to the class and stamps both onto the wire"
                + " envelope",
            arch => DcaRule.Fail(
                "Integration Events must be annotated with [IntegrationEventType]:",
                Violations(
                    arch,
                    t => t is not Interface && IsIntegrationEvent(t),
                    t => !t.HasAttribute(typeof(IntegrationEventTypeAttribute).FullName!),
                    t => $"{t.FullName} is not annotated with [IntegrationEventType]")));

    public IDcaRule IntegrationEventsHaveNoVersionField() =>
        DcaRule.Check(
            "DCA-ADV-006",
            "Integration Events must not have a version field",
            "The schema version is a class property ([IntegrationEventType]), never per-instance payload"
                + " data — a version data field duplicates the attribute and can drift from it",
            arch => DcaRule.Fail(
                "Integration Events must not have a version field — [IntegrationEventType] is the"
                    + " single source of truth:",
                Violations(
                    arch,
                    t => t is not Interface && IsIntegrationEvent(t),
                    t => HasVersionField(arch, t),
                    t => $"{t.FullName} carries a version data field — declare the version in"
                        + " [IntegrationEventType] instead")));

    public IDcaRule DomainOnlyEventsHaveNoVersionField() =>
        DcaRule.Check(
            "DCA-ADV-007",
            "Domain Events that are not Integration Events must not have a version field",
            "Versioning is a contract concern of integration events — a purely internal domain event"
                + " has no wire contract to version",
            arch => DcaRule.Fail(
                "Domain Events (non-IIntegrationEvent) must not have a version field — versioning is"
                    + " only for IIntegrationEvents:",
                Violations(
                    arch,
                    t => t is not Interface && IsDomainEvent(t) && !IsIntegrationEvent(t),
                    t => HasVersionField(arch, t),
                    t => $"{t.FullName} has a version field but is not an IIntegrationEvent — only"
                        + " IIntegrationEvents need versioning")));

    public IDcaRule DomainEventsHaveTimestampField() =>
        DcaRule.Check(
            "DCA-ADV-008",
            "Domain Events must have a timestamp field",
            "An event records something that happened — without a timestamp the fact cannot be"
                + " ordered, replayed or audited",
            arch => DcaRule.Fail(
                "Domain Events must have a timestamp field (when did the event occur?):",
                Violations(
                    arch,
                    t => t is not Interface && IsDomainEvent(t),
                    t => !HasTimestampField(arch, t),
                    t => $"{t.FullName} does not have a timestamp field")));

    // ============================================================================
    // DOMAIN SERVICES PATTERN
    // ============================================================================

    public IDcaRule DomainServicesResideInDomainService() =>
        DcaRule.Of(
            "DCA-ADV-009",
            "Domain Services must implement IDomainService Marker Interface and reside in Domain.Service",
            "Domain services implement IDomainService marker and reside in Domain.Service namespaces"
                + " (named descriptively, e.g., PricingService, CartTotalCalculator)",
            arch => Types()
                .That()
                .AreAssignableTo(typeof(IDomainService))
                .And()
                .FollowCustomPredicate(t => t is not Interface, "are not interfaces")
                .Should()
                .ResideInNamespaceMatching(DomainServicePattern()));

    public IDcaRule DomainServicesResideInDomain() =>
        DcaRule.Of(
            "DCA-ADV-010",
            "Domain Services must reside in domain namespace",
            "Domain services are part of the domain layer, not application layer",
            arch => Types()
                .That()
                .AreAssignableTo(typeof(IDomainService))
                .And()
                .FollowCustomPredicate(t => t is not Interface, "are not interfaces")
                .Should()
                .ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllDomainPatterns())));

    public IDcaRule DomainServicesHaveNoFrameworkAttributes() =>
        DcaRule.Check(
            "DCA-ADV-011",
            "Domain Services must not have framework attributes",
            "Domain services should be framework-independent",
            arch => FailOnFrameworkAttributes(
                arch,
                t => t is not Interface && t.IsAssignableTo(typeof(IDomainService).FullName!),
                "Domain Services must not carry framework attributes:"));

    public IDcaRule DomainServicesAreStateless() =>
        DcaRule.Check(
            "DCA-ADV-012",
            "Domain Services should be stateless (only readonly fields for dependencies)",
            "Domain services should be stateless (only readonly fields for dependencies)",
            arch => DcaRule.Fail(
                "Domain Services must have only readonly fields:",
                NonReadonlyFieldViolations(
                    arch,
                    t => t is not Interface && InDomain(arch, t) && t.IsAssignableTo(typeof(IDomainService).FullName!))));

    // ============================================================================
    // FACTORIES PATTERN
    // ============================================================================

    public IDcaRule FactoriesAreNamedFactory() =>
        DcaRule.Of(
            "DCA-ADV-013",
            "Factories should implement IFactory Marker Interface",
            "Classes implementing IFactory marker should have 'Factory' in their name",
            arch => Types()
                .That()
                .AreAssignableTo(typeof(IFactory))
                .And()
                .FollowCustomPredicate(t => t is not Interface, "are not interfaces")
                .Should()
                .HaveNameEndingWith("Factory"));

    public IDcaRule FactoriesResideInDomain() =>
        DcaRule.Of(
            "DCA-ADV-014",
            "Factories must reside in domain namespace",
            "Factories are part of the domain layer (complex aggregate creation logic)",
            arch => Types()
                .That()
                .AreAssignableTo(typeof(IFactory))
                .And()
                .FollowCustomPredicate(t => t is not Interface, "are not interfaces")
                .Should()
                .ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllDomainPatterns())));

    public IDcaRule FactoriesHaveNoFrameworkAttributes() =>
        DcaRule.Check(
            "DCA-ADV-015",
            "Factories must not have framework attributes",
            "Factories should be framework-independent",
            arch => FailOnFrameworkAttributes(
                arch,
                t => t is not Interface && InDomain(arch, t) && t.IsAssignableTo(typeof(IFactory).FullName!),
                "Factories must not carry framework attributes:"));

    public IDcaRule FactoriesAreStateless() =>
        DcaRule.Check(
            "DCA-ADV-016",
            "Factories should be stateless (only readonly fields for dependencies)",
            "Factories should be stateless (only readonly fields for dependencies)",
            arch => DcaRule.Fail(
                "Factories must have only readonly fields:",
                NonReadonlyFieldViolations(
                    arch,
                    t => t is not Interface && InDomain(arch, t) && t.IsAssignableTo(typeof(IFactory).FullName!))));

    // ============================================================================
    // SPECIFICATION PATTERN
    // ============================================================================

    public IDcaRule SpecificationsResideInDomain() =>
        DcaRule.Of(
            "DCA-ADV-017",
            "Specifications must end with 'Specification'",
            "Specification implementations are part of the domain layer",
            arch => Types()
                .That()
                .HaveNameEndingWith("Specification")
                .And()
                .FollowCustomPredicate(t => t is not Interface, "are not interfaces")
                .And()
                .DoNotHaveName("Specification")
                .Should()
                .ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllDomainPatterns())));

    public IDcaRule SpecificationsHaveNoFrameworkAttributes() =>
        DcaRule.Check(
            "DCA-ADV-018",
            "Specifications must not have framework attributes",
            "Specifications should be framework-independent value objects",
            arch => FailOnFrameworkAttributes(
                arch,
                t => InDomain(arch, t) && t.Name.EndsWith("Specification", StringComparison.Ordinal),
                "Specifications must not carry framework attributes:"));

    // ============================================================================
    // HELPERS
    // ============================================================================

    private string DomainServicePattern() =>
        $"^.*\\.{Regex.Escape(Layout.DomainSegment)}\\.Service(\\..*)?$";

    private static bool InDomain(DcaArchitecture arch, IType type) =>
        type.Namespace is not null && Regex.IsMatch(type.Namespace.FullName, DcaLayout.AnyOf(arch.AllDomainPatterns()));

    private static bool IsDomainEvent(IType type) => type.IsAssignableTo(typeof(IDomainEvent).FullName!);

    private static bool IsIntegrationEvent(IType type) => type.IsAssignableTo(typeof(IIntegrationEvent).FullName!);

    /// <summary>A C# record (<c>record class</c>) or a struct (<c>record struct</c>s are structs to ArchUnitNET).</summary>
    private static bool IsRecordLike(IType type) => type is Struct || (type is Class c && c.IsRecord == true);

    /// <summary>All instance and static fields declared on the type or an ancestor (Java <c>getAllFields</c>).</summary>
    private static IEnumerable<FieldInfo> AllFields(DcaArchitecture arch, IType type)
    {
        const BindingFlags flags =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        for (var runtime = arch.RuntimeType(type); runtime is not null && runtime != typeof(object); runtime = runtime.BaseType)
        {
            foreach (var field in runtime.GetFields(flags))
            {
                yield return field;
            }
        }
    }

    /// <summary>Plain field name — an auto-property backing field <c>&lt;Version&gt;k__BackingField</c> counts as <c>Version</c>.</summary>
    private static string LogicalName(FieldInfo field)
    {
        var name = field.Name;
        if (name.StartsWith("<", StringComparison.Ordinal))
        {
            var end = name.IndexOf('>', StringComparison.Ordinal);
            if (end > 1)
            {
                return name.Substring(1, end - 1);
            }
        }

        return name;
    }

    private static bool HasVersionField(DcaArchitecture arch, IType type) =>
        AllFields(arch, type).Any(f => string.Equals(LogicalName(f), VersionField, StringComparison.OrdinalIgnoreCase));

    private static bool HasTimestampField(DcaArchitecture arch, IType type) =>
        AllFields(arch, type).Any(f => TimestampTypes.Contains(f.FieldType.FullName));

    private static List<string> NonReadonlyFieldViolations(DcaArchitecture arch, Func<IType, bool> candidate)
    {
        var violations = new List<string>();
        foreach (var type in arch.Types.Where(candidate))
        {
            foreach (var field in AllFields(arch, type).Where(f => !f.IsInitOnly && !f.IsLiteral))
            {
                violations.Add($"{type.FullName} has non-readonly field '{LogicalName(field)}' — a stateless service/factory"
                    + " holds only readonly dependencies");
            }
        }

        return violations;
    }

    /// <summary>
    /// Attributes whose type comes from outside the domain's allowed dependencies (<c>System</c>, the
    /// building blocks, <see cref="DcaLayout.ThirdPartyNamespacesAllowedInDomain"/>) or the domain layer itself
    /// are framework attributes — the .NET reading of "no Spring stereotype annotations".
    /// </summary>
    private void FailOnFrameworkAttributes(DcaArchitecture arch, Func<IType, bool> candidate, string header)
    {
        var violations = new List<string>();
        foreach (var type in arch.Types.Where(candidate))
        {
            var runtime = arch.RuntimeType(type);
            if (runtime is null)
            {
                continue;
            }

            foreach (var attribute in runtime.GetCustomAttributesData())
            {
                var attributeType = attribute.AttributeType;
                if (!IsAllowedAttribute(arch, attributeType))
                {
                    violations.Add($"{type.FullName} is annotated with framework attribute [{attributeType.FullName}]");
                }
            }
        }

        DcaRule.Fail(header, violations, "Register the type in the container by code; keep the domain free of framework attributes.");
    }

    private bool IsAllowedAttribute(DcaArchitecture arch, Type attributeType)
    {
        var ns = attributeType.Namespace ?? string.Empty;
        return Layout.ThirdPartyNamespacesAllowedInDomain.Any(prefix => DcaLayout.IsBelow(ns, prefix))
            || DcaLayout.IsBelow(ns, DcaLayout.BuildingBlocksNamespace)
            || Regex.IsMatch(ns, DcaLayout.AnyOf(arch.AllDomainPatterns()));
    }

    private static List<string> Violations(
        DcaArchitecture arch,
        Func<IType, bool> candidate,
        Func<IType, bool> violates,
        Func<IType, string> message) =>
        arch.Types.Where(t => candidate(t) && violates(t)).Select(message).ToList();
}
