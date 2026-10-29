using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Dependencies;
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
            DomainServicesResideInDomain(),
            DomainServicesHaveNoFrameworkAttributes(),
            DomainServicesAreStateless(),
            FactoriesAreNamedFactory(),
            FactoriesResideInDomain(),
            FactoriesHaveNoFrameworkAttributes(),
            FactoriesAreStateless(),
            SpecificationsResideInDomain(),
            SpecificationsHaveNoFrameworkAttributes(),
            ModelTypesDoNotUseDomainServices(),
            DomainServiceOperationsTakeAModelObject(),
        };
    }

    /// <summary>DCA-ADV-019 — the model does not reach for a domain service.</summary>
    public IDcaRule ModelTypesDoNotUseDomainServices() =>
        DcaRule.Check(
            "DCA-ADV-019",
            "Aggregates, entities and value objects must not use domain services",
            "A domain service exists for logic that spans several aggregates, so the application calls it; "
                + "a model type that reaches for one would drive another aggregate from inside its own",
            arch =>
            {
                var violations = new List<string>();
                foreach (var type in arch.Types.Where(t => IsModelType(arch, t)))
                {
                    foreach (var target in type.Dependencies
                        .Where(d => d is MethodCallDependency)
                        .Select(d => d.Target)
                        .Distinct())
                    {
                        if (arch.Layout.Markers.Is(nameof(DcaMarkers.DomainService), target))
                        {
                            violations.Add($"{type.FullName} uses {target.FullName}");
                        }
                    }
                }

                DcaRule.Fail("A model type reaches for a domain service", violations.Distinct().ToList(),
                    "let the use case call the domain service and hand the model the result");
            })
            .Selecting("Types anywhere under scan that carry the aggregate-root, entity or value role.")
            .Checking(
                "None of them calls a method of, or constructs, a type carrying the domain-service role. "
                + "Holding one as a parameter or a field type is not a call and is not reported. A domain "
                + "service calling another domain service is outside this selection. An empty selection passes.");

    /// <summary>DCA-ADV-020 — a domain service works on the model, not on extracted values.</summary>
    public IDcaRule DomainServiceOperationsTakeAModelObject() =>
        DcaRule.Check(
            "DCA-ADV-020",
            "Domain service operations take an aggregate or an entity",
            "A domain service works on the model itself; an operation that only takes extracted values "
                + "moves the decision out of the domain and into its caller",
            arch =>
            {
                var violations = new List<string>();
                foreach (var service in arch.Types.Where(t =>
                    t is not Interface && arch.Layout.Markers.Is(nameof(DcaMarkers.DomainService), t)))
                {
                    foreach (var method in service.GetMethodMembers()
                        .Where(m => m.MethodForm == MethodForm.Normal && m.Visibility == Visibility.Public))
                    {
                        if (ObjectMethods.Contains(SimpleName(method)))
                        {
                            continue;
                        }

                        var takesModelObject = method.Parameters.Any(parameter =>
                            arch.Layout.Markers.Is(nameof(DcaMarkers.AggregateRoot), parameter)
                            || arch.Layout.Markers.Is(nameof(DcaMarkers.Entity), parameter));
                        if (!takesModelObject)
                        {
                            violations.Add($"{service.FullName}.{SimpleName(method)} takes no aggregate and no entity");
                        }
                    }
                }

                DcaRule.Fail("A domain service operation takes at least one aggregate or entity", violations,
                    "pass the aggregate itself, or move the operation onto the value object it computes on");
            })
            .Selecting(
                "Public methods declared on non-interface types carrying the domain-service role, except the "
                + "methods every object has (Equals, GetHashCode, ToString) and compiler generated ones.")
            .Checking(
                "At least one parameter of the method carries the aggregate-root or the entity role. Value "
                + "objects and foreign facts may be passed alongside but do not satisfy the check on their "
                + "own: a calculation that needs no aggregate is behaviour of the value object or the "
                + "aggregate, not a domain service. An empty selection passes.");

    public string Name => "advanced";

    public IReadOnlyList<IDcaRule> Rules { get; }

    /// <summary>The layout this rule set was built for.</summary>
    public DcaLayout Layout { get; }

    private static readonly HashSet<string> ObjectMethods =
        new(StringComparer.Ordinal) { "Equals", "GetHashCode", "ToString" };

    /// <summary>ArchUnitNET member names carry the parameter list; this strips it.</summary>
    private static string SimpleName(MethodMember method)
    {
        var paren = method.Name.IndexOf('(', StringComparison.Ordinal);
        return paren < 0 ? method.Name : method.Name.Substring(0, paren);
    }

    private static bool IsModelType(DcaArchitecture arch, IType type) =>
        arch.Layout.Markers.Is(nameof(DcaMarkers.AggregateRoot), type)
        || arch.Layout.Markers.Is(nameof(DcaMarkers.Entity), type)
        || arch.Layout.Markers.Is(nameof(DcaMarkers.Value), type);

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
                    t => t is not Interface && IsDomainEvent(t, arch.Layout.Markers),
                    t => !TacticalPatternRules.IsImmutableShape(t),
                    t => $"{t.FullName} implements IDomainEvent but has mutable shape")))
            .Selecting(
                "Non-interface types anywhere under scan that are assignable to IDomainEvent - directly "
                + "or through a supertype.")
            .Checking(
            "Classes are sealed or records; structs are allowed. Every inherited instance field is readonly, every property is get-only or init-only and no instance Set*(x): void method exists. Referenced objects and collection contents are not inspected.");

    public IDcaRule DomainEventsResideInDomain() =>
        DcaRule.Of(
            "DCA-ADV-002",
            "Domain Events must reside in domain namespace",
            "Domain events are part of the domain layer (named in past tense)",
            arch => Types()
                .That()
                .FollowCustomPredicate(t => arch.Layout.Markers.Is(nameof(DcaMarkers.DomainEvent), t), "are domain events")
                .And()
                .FollowCustomPredicate(t => t is not Interface, "are not interfaces")
                .Should()
                .FollowCustomCondition(LayerConditions.PlacedIn(arch.InDomain, "the domain layer"), "be in the domain layer"))
            .Selecting(
                "Non-interface types anywhere under scan that are assignable to IDomainEvent.")
            .Checking(
                "Each resides in a domain namespace of some module root (<module>.Domain or below). An "
                + "event in an application, adapter or infrastructure namespace is reported. An empty "
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
                    t => t is not Interface && IsIntegrationEvent(t, arch.Layout.Markers),
                    t => !t.HasAttribute(arch.Layout.Markers.IntegrationEventType),
                    t => $"{t.FullName} is not annotated with [IntegrationEventType]")))
            .Selecting(
                "Non-interface types anywhere under scan that are assignable to IIntegrationEvent - "
                + "records, structs and abstract classes included.")
            .Checking(
                "The type itself carries [IntegrationEventType] - or the attribute the IntegrationEventType "
                + "marker role names. An attribute on a supertype does not count; the attribute's name and version values are not checked. An empty selection "
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
                    t => t is not Interface && IsIntegrationEvent(t, arch.Layout.Markers),
                    t => HasVersionField(arch, t),
                    t => $"{t.FullName} carries an explicit schema-version field — declare the version in"
                        + " [IntegrationEventType] instead")))
            .Selecting(
                "Non-interface types anywhere under scan that are assignable to IIntegrationEvent.")
            .Checking("Name heuristic: schemaVersion, eventVersion and contractVersion fields (case-insensitive), declared or inherited, including auto-property backing fields and record parameters, are reported. A business revision named version is allowed, whatever its type. A type the loader cannot resolve is read through the ArchUnitNET member model instead, which does not carry the backing fields of auto-properties. The heuristic cannot infer business meaning; an empty selection passes.");

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
                    t => t is not Interface && IsDomainEvent(t, arch.Layout.Markers) && !IsIntegrationEvent(t, arch.Layout.Markers),
                    t => HasVersionField(arch, t),
                    t => $"{t.FullName} has an explicit schema-version field but is not an IIntegrationEvent — only"
                        + " IIntegrationEvents need schema versioning")))
            .Selecting(
                "Non-interface types anywhere under scan that are assignable to IDomainEvent but not to "
                + "IIntegrationEvent. A type assignable to both is not selected.")
            .Checking("Name heuristic: schemaVersion, eventVersion and contractVersion fields (case-insensitive), declared or inherited, including auto-property backing fields and record parameters, are reported. A business revision named version is allowed, whatever its type. A type the loader cannot resolve is read through the ArchUnitNET member model instead, which does not carry the backing fields of auto-properties. The heuristic cannot infer business meaning; an empty selection passes.");

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
                    t => t is not Interface && IsDomainEvent(t, arch.Layout.Markers),
                    t => !HasTimestampField(arch, t),
                    t => $"{t.FullName} does not have a timestamp field")))
            .Selecting(
                "Non-interface types anywhere under scan that are assignable to IDomainEvent. "
                + "IIntegrationEvent does not extend IDomainEvent, so an integration event is selected only "
                + "when it also implements IDomainEvent.")
            .Checking(
                "At least one field - declared by the type or inherited from a base type, static or not, "
                + "of any name - has one of the configured timestamp types, by default System.DateTimeOffset "
                + "or System.DateTime; an auto-property or "
                + "positional record parameter of one of these types counts through its backing field. "
                + "DateOnly, TimeSpan, long or string fields do not satisfy it, and a computed property "
                + "without a backing field does not either - which is what the rule is for, because the "
                + "marker already forces the accessor; a project whose own event vocabulary wraps the "
                + "timestamp in a value type names that type with WithTimestampTypes. A type the loader "
                + "cannot resolve is read "
                + "through the ArchUnitNET member model instead, which does not carry the backing fields "
                + "of auto-properties. Every offender is reported in one violation; an empty selection "
                + "passes.");

    // ============================================================================
    // DOMAIN SERVICES PATTERN
    // ============================================================================

    public IDcaRule DomainServicesResideInDomain() =>
        DcaRule.Of(
            "DCA-ADV-010",
            "Marked domain services reside in a module domain",
            "Domain services are part of the domain layer, not application layer",
            arch => Types()
                .That()
                .FollowCustomPredicate(t => arch.Layout.Markers.Is(nameof(DcaMarkers.DomainService), t), "are domain services")
                .And()
                .FollowCustomPredicate(t => t is not Interface, "are not interfaces")
                .Should()
                .FollowCustomCondition(LayerConditions.PlacedIn(arch.InDomain, "the domain layer"), "be in the domain layer"))
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
                    t => t is not Interface && InDomain(arch, t) && arch.Layout.Markers.Is(nameof(DcaMarkers.DomainService), t))))
            .Selecting(
                "Non-interface types in <module>.Domain of every module root that are assignable to "
                + "IDomainService.")
            .Checking(
                "Every field - declared by the type or inherited from a base type, static fields included "
                + "- is readonly or const. A settable auto-property is reported through its backing field; "
                + "a get-only one passes. Field types are not inspected, so a readonly field holding "
                + "mutable state passes. A type the loader cannot resolve is read through the "
                + "ArchUnitNET member model instead, and a field whose writability neither model states "
                + "is not reported. An empty selection passes.");

    // ============================================================================
    // FACTORIES PATTERN
    // ============================================================================

    public IDcaRule FactoriesAreNamedFactory() =>
        DcaRule.Of(
            "DCA-ADV-013",
            "Types carrying the factory role are named *Factory",
            "A factory is found by its role, and read by its name: a type that creates aggregates and is"
                + " not called one makes the creation site hard to find in review and in a search",
            arch => Types()
                .That()
                .FollowCustomPredicate(t => arch.Layout.Markers.Is(nameof(DcaMarkers.Factory), t), "are factories")
                .And()
                .FollowCustomPredicate(t => t is not Interface, "are not interfaces")
                .Should()
                .HaveNameEndingWith(arch.Layout.FactorySuffix))
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
                .FollowCustomPredicate(t => arch.Layout.Markers.Is(nameof(DcaMarkers.Factory), t), "are factories")
                .And()
                .FollowCustomPredicate(t => t is not Interface, "are not interfaces")
                .Should()
                .FollowCustomCondition(LayerConditions.PlacedIn(arch.InDomain, "the domain layer"), "be in the domain layer"))
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
                    t => t is not Interface && InDomain(arch, t) && arch.Layout.Markers.Is(nameof(DcaMarkers.Factory), t))))
            .Selecting(
                "Non-interface types in <module>.Domain of every module root that are assignable to "
                + "IFactory.")
            .Checking(
                "Every field - declared by the type or inherited from a base type, static fields included "
                + "- is readonly or const. A settable auto-property is reported through its backing field. "
                + "Field types are not inspected. A type the loader cannot resolve is read through the "
                + "ArchUnitNET member model instead, and a field whose writability neither model states is "
                + "not reported. An empty selection passes.");

    // ============================================================================
    // SPECIFICATION PATTERN
    // ============================================================================

    public IDcaRule SpecificationsResideInDomain() =>
        DcaRule.Of(
            "DCA-ADV-017",
            "Specifications reside in the domain layer",
            "A specification is a rule of the model expressed as a predicate; it belongs where the model"
                + " is, not in the layer that happens to ask the question",
            arch => Types()
                .That()
                .FollowCustomPredicate(
                    t => t is not Interface
                        && t.Name != arch.Layout.SpecificationSuffix
                        && t.FullName != arch.Layout.Markers.Specification
                        && (IsSpecificationRole(arch, t)
                            || t.Name.EndsWith(arch.Layout.SpecificationSuffix, StringComparison.Ordinal)),
                    "are specifications")
                .Should()
                .FollowCustomCondition(LayerConditions.PlacedIn(arch.InDomain, "the domain layer"), "be in the domain layer"))
            .Selecting(
                "Non-interface types anywhere under scan that are assignable to the configured "
                + "specification role or whose simple name ends with Specification, the role's own type "
                + "and a type named exactly Specification excluded. The marker and the name both select, "
                + "so a specification named after the predicate it expresses is governed too.")
            .Checking(
                "Each resides in a domain namespace of some module root (<module>.Domain or below). An "
                + "empty selection passes.");

    public IDcaRule SpecificationsHaveNoFrameworkAttributes() =>
        DcaRule.Check("DCA-ADV-018", "Specifications must not carry prohibited framework metadata",
            "Domain objects carry no metadata for container management, persistence or transaction coordination",
            arch => DomainMetadata.Check(arch, "DCA-ADV-018"))
        .Selecting("Non-interface types in domain namespaces that are assignable to the configured specification role or whose simple name ends with Specification. Exclusive ownership: events, services, factories, specifications, then domain-model types.")
        .Checking("Configured attribute namespaces classify the attribute type or any base type: types prohibit container, persistence and transaction roles; fields and properties prohibit injection and persistence; methods prohibit transaction and, except on events, injection; constructors prohibit injection. There is no default event-listener attribute role. Unclassified attributes are allowed; runtime types that cannot load are skipped.");

    // ============================================================================
    // HELPERS
    // ============================================================================

    /// <summary>
    /// Whether the type carries the configured specification role. Read through the runtime type when
    /// it loads, because the role names the marker without its arity (<c>ISpecification</c>) while a
    /// reflected generic interface carries it (<c>ISpecification`1</c>).
    /// </summary>
    internal static bool IsSpecificationRole(DcaArchitecture arch, IType type)
    {
        if (type.IsGenericParameter)
        {
            return false;
        }

        var runtime = arch.RuntimeType(type);
        return runtime is not null
            ? arch.Layout.Markers.Is(nameof(DcaMarkers.Specification), runtime)
            : arch.Layout.Markers.Is(nameof(DcaMarkers.Specification), type);
    }

    private static bool InDomain(DcaArchitecture arch, IType type) => arch.InDomain(type);

    private static bool IsDomainEvent(IType type, DcaMarkers markers) => markers.Is(nameof(DcaMarkers.DomainEvent), type);

    private static bool IsIntegrationEvent(IType type, DcaMarkers markers) => markers.Is(nameof(DcaMarkers.IntegrationEvent), type);

    /// <summary>A C# record (<c>record class</c>) or a struct (<c>record struct</c>s are structs to ArchUnitNET).</summary>
    private static bool IsRecordLike(IType type) => type is Struct || (type is Class c && c.IsRecord == true);

    /// <summary>
    /// One field, as the rules need it, from whichever model can see it. <see cref="Writable"/> is
    /// <c>null</c> when neither model says — an unknown is never reported.
    /// </summary>
    private readonly record struct FieldFact(string Name, string? TypeFullName, bool? Writable);

    /// <summary>
    /// All instance and static fields declared on the type or an ancestor (Java <c>getAllFields</c>).
    /// Read through the CLR when the type loads, because reflection sees the compiler-generated
    /// backing fields of auto-properties; through the ArchUnitNET member model when it does not, so a
    /// type the loader cannot resolve still changes the verdict it should change.
    /// </summary>
    private static IEnumerable<FieldFact> AllFields(DcaArchitecture arch, IType type)
    {
        var runtime = arch.RuntimeType(type);
        if (runtime is not null)
        {
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (var current = runtime; current is not null && current != typeof(object); current = current.BaseType)
            {
                foreach (var field in current.GetFields(flags))
                {
                    yield return new FieldFact(LogicalName(field.Name), field.FieldType.FullName, !field.IsInitOnly && !field.IsLiteral);
                }
            }

            yield break;
        }

        for (IType? current = type; current is not null; current = (current as Class)?.BaseClass)
        {
            foreach (var field in current.Members.OfType<FieldMember>())
            {
                yield return new FieldFact(
                    LogicalName(field.Name),
                    field.Type?.FullName,
                    field.Writability is { } writability ? writability == Writability.Writable : null);
            }
        }
    }

    /// <summary>Plain field name — an auto-property backing field <c>&lt;Version&gt;k__BackingField</c> counts as <c>Version</c>.</summary>
    private static string LogicalName(string name)
    {
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
        AllFields(arch, type).Any(f => SchemaFields.Contains(f.Name, StringComparer.OrdinalIgnoreCase));

    private static bool HasTimestampField(DcaArchitecture arch, IType type) =>
        AllFields(arch, type).Any(f =>
            f.TypeFullName is not null && arch.Layout.TimestampTypes.Contains(f.TypeFullName));

    private static List<string> NonReadonlyFieldViolations(DcaArchitecture arch, Func<IType, bool> candidate)
    {
        var violations = new List<string>();
        foreach (var type in arch.Types.Where(candidate))
        {
            foreach (var field in AllFields(arch, type).Where(f => f.Writable == true))
            {
                violations.Add($"{type.FullName} has non-readonly field '{field.Name}' — a stateless service/factory"
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
