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
    private static readonly string[] SchemaFields = { "schemaVersion", "eventVersion", "contractVersion" };

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
            "Domain Events must implement IDomainEvent and have immutable shape",
            "Domain events should be immutable records implementing IDomainEvent (named in past tense,"
                + " e.g., ProductCreated, CartCleared)",
            arch => DcaRule.Fail(
                "Domain Events must have immutable instance state:",
                Violations(
                    arch,
                    t => t is not Interface && IsDomainEvent(t),
                    t => !TacticalPatternRules.IsImmutableShape(t),
                    t => $"{t.FullName} implements IDomainEvent but has mutable shape")))
            .Selecting(
                "Non-interface types anywhere under scan that are assignable to IDomainEvent - directly "
                + "or through a supertype.")
            .Checking(
            "Classes are sealed or records; structs are allowed. Every inherited instance field is readonly, every property is get-only or init-only and no instance Set*(x): void method exists. Referenced objects and collection contents are not inspected." );

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
                .ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllDomainPatterns())))
            .Selecting(
                "Non-interface types anywhere under scan that are assignable to IDomainEvent.")
            .Checking(
                "Each resides in a domain namespace of some module root (<module>.Domain or below). An "
                + "event in an application, adapter or infrastructure namespace is reported. An empty "
                + "selection passes.");

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
                    t => $"{t.FullName} is neither a record nor sealed")))
            .Selecting(
                "Non-record classes in <module>.Domain of every module root that are assignable to "
                + "IDomainEvent. Records, structs and interfaces are not selected.")
            .Checking(
                "The class is sealed. A record event is not selected, so it always passes here. An empty "
                + "selection passes.");

    public IDcaRule DomainEventsHaveNoFrameworkAttributes() =>
        DcaRule.Check("DCA-ADV-004", "Domain events must not carry prohibited framework metadata",
            "Domain objects carry no metadata for container management, persistence or transaction coordination",
            arch => DomainMetadata.Check(arch, "DCA-ADV-004"))
        .Selecting("Non-interface domain events in domain namespaces. Exclusive ownership: events, services, factories, specifications, then domain-model types.")
        .Checking("Configured attribute namespaces classify the attribute type or any base type: types prohibit container, persistence and transaction roles; fields and properties prohibit injection and persistence; methods prohibit transaction and, except on events, injection; constructors prohibit injection. There is no default event-listener attribute role. Unclassified attributes are allowed; runtime types that cannot load are skipped.");

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
                    t => $"{t.FullName} is not annotated with [IntegrationEventType]")))
            .Selecting(
                "Non-interface types anywhere under scan that are assignable to IIntegrationEvent - "
                + "records, structs and abstract classes included.")
            .Checking(
                "The type itself carries [IntegrationEventType]. An attribute on a supertype does not "
                + "count; the attribute's name and version values are not checked. An empty selection "
                + "passes.");

    public IDcaRule IntegrationEventsHaveNoVersionField() =>
        DcaRule.Check(
            "DCA-ADV-006",
            "Integration events carry the schema version in their type metadata, not in the payload",
            "The schema version is a class property ([IntegrationEventType]), never per-instance payload"
                + " data — an explicit schema-version data field duplicates the attribute and can drift from it",
            arch => DcaRule.Fail(
                "Integration events carry the schema version in their type metadata, not in the payload — [IntegrationEventType] is the"
                    + " single source of truth:",
                Violations(
                    arch,
                    t => t is not Interface && IsIntegrationEvent(t),
                    t => HasVersionField(arch, t),
                    t => $"{t.FullName} carries an explicit schema-version field — declare the version in"
                        + " [IntegrationEventType] instead")))
            .Selecting(
                "Non-interface types anywhere under scan that are assignable to IIntegrationEvent.")
            .Checking("Name heuristic: schemaVersion, eventVersion and contractVersion fields (case-insensitive), declared or inherited, including auto-property backing fields and record parameters, are reported. A business revision named version is allowed, whatever its type. The heuristic cannot infer business meaning; an empty selection passes.");

    public IDcaRule DomainOnlyEventsHaveNoVersionField() =>
        DcaRule.Check(
            "DCA-ADV-007",
            "Domain events that are not integration events carry no schema version",
            "Versioning is a contract concern of integration events — a purely internal domain event"
                + " has no wire contract to version",
            arch => DcaRule.Fail(
                "Domain Events (non-IIntegrationEvent) must not have an explicit schema-version field — schema versioning is"
                    + " only for IIntegrationEvents:",
                Violations(
                    arch,
                    t => t is not Interface && IsDomainEvent(t) && !IsIntegrationEvent(t),
                    t => HasVersionField(arch, t),
                    t => $"{t.FullName} has an explicit schema-version field but is not an IIntegrationEvent — only"
                        + " IIntegrationEvents need schema versioning")))
            .Selecting(
                "Non-interface types anywhere under scan that are assignable to IDomainEvent but not to "
                + "IIntegrationEvent. A type assignable to both is not selected.")
            .Checking("Name heuristic: schemaVersion, eventVersion and contractVersion fields (case-insensitive), declared or inherited, including auto-property backing fields and record parameters, are reported. A business revision named version is allowed, whatever its type. The heuristic cannot infer business meaning; an empty selection passes.");

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
                    t => $"{t.FullName} does not have a timestamp field")))
            .Selecting(
                "Non-interface types anywhere under scan that are assignable to IDomainEvent. "
                + "IIntegrationEvent does not extend IDomainEvent, so an integration event is selected only "
                + "when it also implements IDomainEvent.")
            .Checking(
                "At least one field - declared by the type or inherited from a base type, static or not, "
                + "of any name - has the type System.DateTimeOffset or System.DateTime; an auto-property or "
                + "positional record parameter of one of these types counts through its backing field. "
                + "DateOnly, TimeSpan, long or string fields do not satisfy it, and a computed property "
                + "without a backing field does not either. Every offender is reported in one violation; an "
                + "empty selection passes.");

    // ============================================================================
    // DOMAIN SERVICES PATTERN
    // ============================================================================

    public IDcaRule DomainServicesResideInDomainService() =>
        DcaRule.Of(
            "DCA-ADV-009",
            "Marked domain services reside in the configured domain service segment",
            "Domain services implement IDomainService marker and reside in Domain.Service namespaces"
                + " (named descriptively, e.g., PricingService, CartTotalCalculator)",
            arch => Types()
                .That()
                .AreAssignableTo(typeof(IDomainService))
                .And()
                .FollowCustomPredicate(t => t is not Interface, "are not interfaces")
                .Should()
                .ResideInNamespaceMatching(DomainServicePattern()))
            .Selecting(
                "Non-interface types anywhere under scan that are assignable to IDomainService.")
            .Checking(
                "Each resides in a namespace matching .<domain segment>.Service or below - the configured "
                + "domain segment followed by Service, anywhere in the namespace path, not tied to a module "
                + "root. A domain service directly in Domain or in Domain.Model is reported. An empty "
                + "selection passes.");

    public IDcaRule DomainServicesResideInDomain() =>
        DcaRule.Of(
            "DCA-ADV-010",
            "Marked domain services reside in a module domain",
            "Domain services are part of the domain layer, not application layer",
            arch => Types()
                .That()
                .AreAssignableTo(typeof(IDomainService))
                .And()
                .FollowCustomPredicate(t => t is not Interface, "are not interfaces")
                .Should()
                .ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllDomainPatterns())))
            .Selecting(
                "Non-interface types anywhere under scan that are assignable to IDomainService.")
            .Checking(
                "Each resides in a domain namespace of some module root (<module>.Domain or below). An "
                + "empty selection passes.");

    public IDcaRule DomainServicesHaveNoFrameworkAttributes() =>
        DcaRule.Check("DCA-ADV-011", "Domain services must not carry prohibited framework metadata",
            "Domain objects carry no metadata for container management, persistence or transaction coordination",
            arch => DomainMetadata.Check(arch, "DCA-ADV-011"))
        .Selecting("Non-interface domain services in domain namespaces. Exclusive ownership: events, services, factories, specifications, then domain-model types.")
        .Checking("Configured attribute namespaces classify the attribute type or any base type: types prohibit container, persistence and transaction roles; fields and properties prohibit injection and persistence; methods prohibit transaction and, except on events, injection; constructors prohibit injection. There is no default event-listener attribute role. Unclassified attributes are allowed; runtime types that cannot load are skipped.");

    public IDcaRule DomainServicesAreStateless() =>
        DcaRule.Check(
            "DCA-ADV-012",
            "Domain Services should be stateless (only readonly fields for dependencies)",
            "Domain services should be stateless (only readonly fields for dependencies)",
            arch => DcaRule.Fail(
                "Domain Services must have only readonly fields:",
                NonReadonlyFieldViolations(
                    arch,
                    t => t is not Interface && InDomain(arch, t) && t.IsAssignableTo(typeof(IDomainService).FullName!))))
            .Selecting(
                "Non-interface types in <module>.Domain of every module root that are assignable to "
                + "IDomainService.")
            .Checking(
                "Every field - declared by the type or inherited from a base type, static fields included "
                + "- is readonly or const. A settable auto-property is reported through its backing field; "
                + "a get-only one passes. Field types are not inspected, so a readonly field holding "
                + "mutable state passes. An empty selection passes.");

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
                .HaveNameEndingWith("Factory"))
            .Selecting(
                "Non-interface types anywhere under scan that are assignable to IFactory.")
            .Checking(
                "The simple name ends with Factory. Only the suffix is checked. An empty selection "
                + "passes.");

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
                .ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllDomainPatterns())))
            .Selecting(
                "Non-interface types anywhere under scan that are assignable to IFactory.")
            .Checking(
                "Each resides in a domain namespace of some module root (<module>.Domain or below). An "
                + "empty selection passes.");

    public IDcaRule FactoriesHaveNoFrameworkAttributes() =>
        DcaRule.Check("DCA-ADV-015", "Factories must not carry prohibited framework metadata",
            "Domain objects carry no metadata for container management, persistence or transaction coordination",
            arch => DomainMetadata.Check(arch, "DCA-ADV-015"))
        .Selecting("Non-interface factories in domain namespaces. Exclusive ownership: events, services, factories, specifications, then domain-model types.")
        .Checking("Configured attribute namespaces classify the attribute type or any base type: types prohibit container, persistence and transaction roles; fields and properties prohibit injection and persistence; methods prohibit transaction and, except on events, injection; constructors prohibit injection. There is no default event-listener attribute role. Unclassified attributes are allowed; runtime types that cannot load are skipped.");

    public IDcaRule FactoriesAreStateless() =>
        DcaRule.Check(
            "DCA-ADV-016",
            "Factories should be stateless (only readonly fields for dependencies)",
            "Factories should be stateless (only readonly fields for dependencies)",
            arch => DcaRule.Fail(
                "Factories must have only readonly fields:",
                NonReadonlyFieldViolations(
                    arch,
                    t => t is not Interface && InDomain(arch, t) && t.IsAssignableTo(typeof(IFactory).FullName!))))
            .Selecting(
                "Non-interface types in <module>.Domain of every module root that are assignable to "
                + "IFactory.")
            .Checking(
                "Every field - declared by the type or inherited from a base type, static fields included "
                + "- is readonly or const. A settable auto-property is reported through its backing field. "
                + "Field types are not inspected. An empty selection passes.");

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
                .ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllDomainPatterns())))
            .Selecting(
                "Non-interface types anywhere under scan whose simple name ends with Specification, "
                + "excluding a type named exactly Specification. No marker is involved - only the name "
                + "selects.")
            .Checking(
                "Each resides in a domain namespace of some module root (<module>.Domain or below). An "
                + "empty selection passes.");

    public IDcaRule SpecificationsHaveNoFrameworkAttributes() =>
        DcaRule.Check("DCA-ADV-018", "Specifications must not carry prohibited framework metadata",
            "Domain objects carry no metadata for container management, persistence or transaction coordination",
            arch => DomainMetadata.Check(arch, "DCA-ADV-018"))
        .Selecting("Non-interface specifications in domain namespaces. Exclusive ownership: events, services, factories, specifications, then domain-model types.")
        .Checking("Configured attribute namespaces classify the attribute type or any base type: types prohibit container, persistence and transaction roles; fields and properties prohibit injection and persistence; methods prohibit transaction and, except on events, injection; constructors prohibit injection. There is no default event-listener attribute role. Unclassified attributes are allowed; runtime types that cannot load are skipped.");

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
        AllFields(arch, type).Any(f => SchemaFields.Contains(LogicalName(f), StringComparer.OrdinalIgnoreCase));

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

    private static List<string> Violations(
        DcaArchitecture arch,
        Func<IType, bool> candidate,
        Func<IType, bool> violates,
        Func<IType, string> message) =>
        arch.Types.Where(t => candidate(t) && violates(t)).Select(message).ToList();
}
