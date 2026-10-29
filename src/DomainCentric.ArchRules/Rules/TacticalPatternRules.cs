using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using Enum = ArchUnitNET.Domain.Enum;
using Type = System.Type;

namespace DomainCentric.ArchRules.Rules;

/// <summary>
/// DDD tactical patterns (building blocks): Aggregate Roots, Entities, Value Objects, Repositories,
/// Stores and enriched read models.
/// </summary>
/// <remarks>
/// References: Evans, <i>Domain-Driven Design</i> (2003); Vernon, <i>Implementing DDD</i> (2013)
/// — especially the four Rules of Aggregate Design; Millett/Tune, <i>Patterns, Principles, and
/// Practices of DDD</i> (2015).
/// </remarks>
public sealed class TacticalPatternRules : IDcaRuleSet
{

    /// <summary>
    /// Write vocabulary a Store must not declare, compared case-insensitively so that the same six
    /// names are matched in both languages — <c>Save</c> and <c>save</c> are the same name.
    /// </summary>
    private static readonly HashSet<string> RepositoryMethodNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Save", "DeleteById", "Delete",
        "SaveAsync", "DeleteByIdAsync", "DeleteAsync",
    };

    /// <summary>Rule ids of the Java catalog that have no .NET reading (none in this set).</summary>
    public static readonly IReadOnlyDictionary<string, string> NotApplicable = new Dictionary<string, string>();

    public TacticalPatternRules(DcaLayout layout)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Rules = new List<IDcaRule>
        {
            AggregateRootsImplementMarker(layout),
            AggregateRootsHoldNoOutputPorts(),
            AggregateRootsReferenceOtherAggregatesById(),
            EntitiesHaveIdField(),
            EntitiesHaveNoPublicConstructors(),
            DomainModelHasNoPublicSetters(),
            EntitiesReferenceAggregatesById(),
            ValueObjectsContainNoEntities(),
            ValueObjectClassesAreFinal(layout),
            ValueObjectFieldsAreFinal(),
            ValueObjectsHaveNoSetters(),
            ValueObjectsAreRecordsOrHaveAttributeEquality(),
            RepositoryInterfacesExtendMarker(layout),
            RepositoryInterfacesResideInSharedOutputPorts(layout),
            RepositoryImplementationsResideInOutgoingAdapters(layout),
            RepositoriesOnlyForAggregateRoots(),
            RepositoriesReturnNoNonRootEntities(),
            StoreInterfacesExtendStoreMarker(),
            StoreInterfacesResideInSharedOutputPorts(layout),
            StoreImplementationsResideInOutgoingAdapters(layout),
            StoreInterfacesHaveNoRepositorySemantics(),
            AggregateRootsHaveNoPublicConstructors(),
            ValueObjectsHaveNoNeutralFactoryBesideAnOriginFactory(),
            DomainHoldsNoBarePrimitives(),
        }.AsReadOnly();
    }

    public string Name => "tactical";

    public IReadOnlyList<IDcaRule> Rules { get; }

    /// <summary>The layout this rule set was built for.</summary>
    public DcaLayout Layout { get; }

    // ---------------------------------------------------------------------------------------------
    // Aggregate Root pattern
    // ---------------------------------------------------------------------------------------------

    public static IDcaRule AggregateRootsImplementMarker(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-TAC-001",
            "Aggregate Roots must implement IAggregateRoot",
            "Classes named *AggregateRoot must implement the IAggregateRoot interface (DDD pattern)",
            arch =>
            {
                var violations = new List<string>();
                foreach (var type in NonInterfaceTypes(arch))
                {
                    if (!type.Name.EndsWith(arch.Layout.AggregateRootSuffix, StringComparison.Ordinal)
                        || type.Name == arch.Layout.AggregateRootSuffix
                        || !ResidesInAny(type, arch.AllDomainModelPatterns()))
                    {
                        continue;
                    }

                    if (!IsAssignableTo(arch, type, arch.Layout.Markers.AggregateRoot))
                    {
                        violations.Add($"{type.FullName} is named *AggregateRoot but does not implement {nameof(IAggregateRoot)}");
                    }
                }

                DcaRule.Fail("Classes named *AggregateRoot must implement the IAggregateRoot marker.", violations);
            })
            .Selecting(
                "Non-interface types - classes, structs and enums - in <module>.Domain.Model of "
                + "every module root whose name ends with AggregateRoot; the type named exactly "
                + "AggregateRoot is excluded.")
            .Checking(
                "The type implements the IAggregateRoot marker. Only the name suffix triggers "
                + "selection - an aggregate root not named *AggregateRoot is never reported, "
                + "and an empty selection passes.");

    public static IDcaRule AggregateRootsHoldNoOutputPorts() =>
        DcaRule.Check(
            "DCA-TAC-002",
            "Aggregate Roots must not hold references to Repositories or other Output Ports",
            "Aggregates are persistence-ignorant: use cases retrieve facts; external calculations belong"
            + " in domain services over supplied snapshots. Review callback parameters manually; this field check cannot prove semantic responsibility",
            arch =>
            {
                var violations = new List<string>();
                foreach (var aggregate in ConcreteTypesAssignableTo(arch, arch.Layout.Markers.AggregateRoot))
                {
                    foreach (var member in DataMembers(arch, aggregate))
                    {
                        var fieldType = member.Type;
                        if (IsAssignableTo(arch, fieldType, arch.Layout.Markers.Repository) || IsAssignableTo(arch, fieldType, arch.Layout.Markers.OutputPort))
                        {
                            violations.Add($"{FieldDescription(aggregate, member)} which is a repository/output port");
                        }
                    }
                }

                DcaRule.Fail(
                    "Aggregates must not have injected repositories or output ports; pass facts instead.",
                    violations);
            })
            .Selecting(
                "Non-interface types below the root namespace assignable to IAggregateRoot, "
                + "abstract ones included.")
            .Checking(
                "No field or property of the type - inherited and static ones included, a static port "
                + "breaks persistence ignorance just the same; record plumbing skipped - has a type "
                + "assignable to IRepository or to any "
                + "other IOutputPort. Only the member's own type is inspected; a port hidden in "
                + "a generic type argument is not seen. A port passed as a method parameter is "
                + "not a member and passes.");

    public static IDcaRule AggregateRootsReferenceOtherAggregatesById() =>
        DcaRule.Check(
            "DCA-TAC-003",
            "Aggregate Roots must not have fields with other Aggregate Root types",
            "Vernon's Aggregate Design Rule #3: reference other Aggregates by identity to keep"
            + " aggregate boundaries and transactional consistency intact",
            arch =>
            {
                var violations = new List<string>();
                foreach (var aggregate in ConcreteTypesAssignableTo(arch, arch.Layout.Markers.AggregateRoot))
                {
                    foreach (var member in InstanceDataMembers(arch, aggregate))
                    {
                        if (IsConcreteAggregateRoot(arch, member.Type))
                        {
                            violations.Add($"{FieldDescription(aggregate, member)} which is another aggregate root");
                        }

                        foreach (var element in member.ElementTypes)
                        {
                            if (IsConcreteAggregateRoot(arch, element))
                            {
                                violations.Add($"{ContainsDescription(aggregate, member, element)} which is an aggregate root");
                            }
                        }
                    }
                }

                DcaRule.Fail("Aggregates must reference other aggregates by ID only (Vernon's Rule #3).", violations);
            })
            .Selecting(
                "Non-interface types below the root namespace assignable to IAggregateRoot, "
                + "abstract ones included.")
            .Checking(
            "No instance field or property - inherited ones included, static ones excluded - involves IAggregateRoot through its type, arrays or nested generic arguments. Same-type references and interfaces extending the marker are included. Interfaces that do not extend the marker are invisible; references by id are valid.");

    // ---------------------------------------------------------------------------------------------
    // Entity pattern
    // ---------------------------------------------------------------------------------------------

    public static IDcaRule EntitiesHaveIdField() =>
        DcaRule.Check(
            "DCA-TAC-004",
            "Entities must have an ID field",
            "An Entity is defined by its identity, which is a value object implementing the IId marker",
            arch =>
            {
                var violations = new List<string>();
                foreach (var entity in ConcreteTypesAssignableTo(arch, arch.Layout.Markers.Entity))
                {
                    if (entity is Class { IsAbstract: true })
                    {
                        continue;
                    }

                    var hasIdMember = DataMembers(arch, entity).Any(m => IsAssignableTo(arch, m.Type, arch.Layout.Markers.Id))
                        || RuntimeDataMemberTypes(arch, entity).Any(t => DcaMarkers.IsAssignableToByName(t, arch.Layout.Markers.Id));
                    if (!hasIdMember)
                    {
                        violations.Add($"{entity.FullName} has no field or property whose type implements {nameof(IId)}");
                    }
                }

                DcaRule.Fail("Entities must have an identity field typed as an IId value object (DDD pattern).", violations);
            })
            .Selecting(
                "Non-interface, non-abstract types below the root namespace assignable to "
                + "IEntity - aggregate roots included, since IAggregateRoot extends IEntity.")
            .Checking(
                "At least one field or property - inherited ones included, private fields of "
                + "base classes seen through reflection - has a type assignable to the IId "
                + "marker. A string or Guid identity does not count, and the member's name "
                + "plays no role.");

    public static IDcaRule EntitiesHaveNoPublicConstructors() =>
        DcaRule.Check(
            "DCA-TAC-005",
            "Entities must not be instantiated directly from outside the aggregate",
            "Entities are created through their aggregate root so that the root can enforce its invariants",
            arch =>
            {
                var violations = new List<string>();
                var entities = NonRootEntities(arch).Select(e => arch.RuntimeType(e)).Where(t => t is not null).ToHashSet();
                foreach (var caller in arch.Types.Where(t => !t.IsCompilerGenerated))
                {
                    var runtime = arch.RuntimeType(caller);
                    if (runtime is null)
                    {
                        continue;
                    }

                    foreach (var target in new IntraClassCalls(runtime).Units.SelectMany(IntraClassCalls.IlCalls)
                        .OfType<ConstructorInfo>().Where(c => entities.Contains(c.DeclaringType)).Distinct())
                    {
                        var entity = target.DeclaringType!;
                        var root = arch.ModuleRootOf(entity.Namespace ?? "");
                        var domain = root + "." + arch.Layout.DomainSegment;
                        var ns = runtime.Namespace ?? "";
                        var sameDomain = root is not null && root == arch.ModuleRootOf(ns)
                            && (ns == domain || ns.StartsWith(domain + ".", StringComparison.Ordinal));
                        var role = DcaMarkers.IsAssignableToByName(runtime, arch.Layout.Markers.AggregateRoot)
                            || DcaMarkers.IsAssignableToByName(runtime, arch.Layout.Markers.Entity)
                            || DcaMarkers.IsAssignableToByName(runtime, arch.Layout.Markers.Factory);
                        if (runtime != entity && !(sameDomain && role))
                        {
                            violations.Add($"{runtime.FullName} constructs entity {entity.FullName} outside its domain construction boundary");
                        }
                    }
                }
                DcaRule.Fail("Entities are constructed by their own domain aggregate, entity or factory.", violations);
            })
            .Selecting("Constructor calls to non-root IEntity types, records and structs included.")
            .Checking("The caller is the entity itself or an IAggregateRoot, IEntity or IFactory in the same context domain layer. Same aggregate ownership is not decidable: another aggregate in that context passes and needs review. Reconstitution goes through an aggregate or factory; reflection is not inspected.");

    public static IDcaRule DomainModelHasNoPublicSetters() =>
        DcaRule.Check(
            "DCA-TAC-006",
            "Domain model classes must not have public setter methods",
            "Behavior-rich domain models change state through intention-revealing methods from the"
            + " ubiquitous language, never through public property setters",
            arch =>
            {
                var violations = new List<string>();
                foreach (var domainClass in ConcreteTypesAssignableTo(arch, arch.Layout.Markers.Entity))
                {
                    foreach (var setter in Setters(domainClass, publicOnly: true))
                    {
                        violations.Add($"{domainClass.FullName} has public setter '{setter}'");
                    }
                }

                DcaRule.Fail(
                    "Domain model classes must not expose public setters - use intention-revealing methods from the ubiquitous language.",
                    violations);
            })
            .Selecting(
                "Non-interface types below the root namespace assignable to IEntity - "
                + "aggregate roots included.")
            .Checking(
                "No member - inherited ones included - is a public setter: a property with a "
                + "public, writable set accessor (an init accessor does not count), or a public "
                + "method named Set followed by an upper-case letter with exactly one parameter "
                + "and return type void. A non-public setter, a fluent setter returning the "
                + "instance, or a Set-prefixed method with zero or two parameters does not "
                + "count.");

    public static IDcaRule EntitiesReferenceAggregatesById() =>
        DcaRule.Check(
            "DCA-TAC-007",
            "Entities must not have fields with Aggregate Root types",
            "An entity inside an aggregate references other aggregates by identity only, otherwise the aggregate boundary leaks",
            arch =>
            {
                var violations = new List<string>();
                foreach (var entity in NonRootEntities(arch))
                {
                    foreach (var member in InstanceDataMembers(arch, entity))
                    {
                        if (IsConcreteAggregateRoot(arch, member.Type))
                        {
                            violations.Add($"{FieldDescription(entity, member)} which is an aggregate root");
                        }

                        foreach (var element in member.ElementTypes)
                        {
                            if (IsConcreteAggregateRoot(arch, element))
                            {
                                violations.Add($"{ContainsDescription(entity, member, element)} which is an aggregate root");
                            }
                        }
                    }
                }

                DcaRule.Fail("Entities must not contain references to aggregate roots (reference by ID only).", violations);
            })
            .Selecting(
                "Non-interface types below the root namespace assignable to IEntity but not "
                + "to IAggregateRoot; records, structs and abstract classes included.")
            .Checking(
            "No instance state, including inherited state, arrays and nested generic arguments, involves IAggregateRoot. Same-type references and interfaces extending the marker are included. Interfaces that do not extend the marker are invisible; references by id are valid.");

    // ---------------------------------------------------------------------------------------------
    // Value Object pattern
    // ---------------------------------------------------------------------------------------------

    public static IDcaRule ValueObjectsContainNoEntities() =>
        DcaRule.Check(
            "DCA-TAC-008",
            "Value Objects must not contain Aggregate Roots or Entities",
            "A Value Object is defined by its attributes; holding an object with identity would give it a lifecycle it must not have",
            arch =>
            {
                var violations = new List<string>();
                foreach (var valueObject in ConcreteTypesAssignableTo(arch, arch.Layout.Markers.Value))
                {
                    foreach (var member in InstanceDataMembers(arch, valueObject))
                    {
                        if (IsConcreteAggregateRoot(arch, member.Type))
                        {
                            violations.Add($"{FieldDescription(valueObject, member)} which is an aggregate root");
                        }

                        if (IsConcreteNonRootEntity(arch, member.Type))
                        {
                            violations.Add($"{FieldDescription(valueObject, member)} which is an entity");
                        }

                        foreach (var element in member.ElementTypes)
                        {
                            if (IsConcreteAggregateRoot(arch, element))
                            {
                                violations.Add($"{ContainsDescription(valueObject, member, element)} which is an aggregate root");
                            }

                            if (IsConcreteNonRootEntity(arch, element))
                            {
                                violations.Add($"{ContainsDescription(valueObject, member, element)} which is an entity");
                            }
                        }
                    }
                }

                DcaRule.Fail("Value Objects must only contain other Value Objects or primitives (Vernon's DDD).", violations);
            })
            .Selecting(
                "Non-interface types below the root namespace assignable to IValue - records, "
                + "structs, enums and hand-written classes alike.")
            .Checking(
            "No instance state, including inherited state, arrays and nested generic arguments, involves IAggregateRoot or IEntity. Same-type references and interfaces extending the marker are included. Interfaces that do not extend the marker are invisible; references by id are valid.");

    public static IDcaRule ValueObjectClassesAreFinal(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-TAC-009",
            "Value Object classes should be sealed (immutability)",
            "Value objects should be immutable and not extensible - Vernon's DDD recommendation;"
            + " .NET: a sealed class, a sealed record, or a struct",
            arch =>
            {
                var violations = new List<string>();
                foreach (var valueObject in arch.Classes)
                {
                    if (valueObject.IsCompilerGenerated
                        || !ResidesInAny(valueObject, arch.AllDomainModelPatterns())
                        || !IsAssignableTo(arch, valueObject, arch.Layout.Markers.Value))
                    {
                        continue;
                    }

                    if (valueObject.IsSealed != true && valueObject.IsAbstract != true)
                    {
                        violations.Add($"{valueObject.FullName} is a Value Object {(valueObject.IsRecord == true ? "record" : "class")} that is not sealed");
                    }
                }

                DcaRule.Fail("Value Object classes and records must be sealed (or be structs).", violations);
            })
            .Selecting(
                "Classes - record classes included, structs and enums not - in "
                + "<module>.Domain.Model of every module root that are assignable to IValue; "
                + "compiler-generated classes excluded.")
            .Checking(
                "The class is sealed or abstract. A non-sealed record class is reported like "
                + "a non-sealed class; structs and enums are never selected and so always pass, "
                + "as does a value object outside <module>.Domain.Model. An empty selection passes.");

    public static IDcaRule ValueObjectFieldsAreFinal() =>
        DcaRule.Check(
            "DCA-TAC-010",
            "Value Object fields must be readonly (shallow immutability)",
            "Records have implicitly init-only state and enums are immutable by design; a hand-written"
            + " value class must make every instance field readonly and every property get-only or init-only itself",
            arch =>
            {
                var violations = new List<string>();
                foreach (var valueObject in ConcreteTypesAssignableTo(arch, arch.Layout.Markers.Value))
                {
                    if (valueObject is Enum)
                    {
                        continue;
                    }

                    foreach (var field in AllMembers(valueObject).OfType<FieldMember>())
                    {
                        if (field.IsCompilerGenerated || field.IsStatic == true)
                        {
                            continue;
                        }

                        if (field.Writability != Writability.ReadOnly)
                        {
                            violations.Add($"{valueObject.FullName} has non-readonly field '{field.Name}'");
                        }
                    }

                    foreach (var property in AllMembers(valueObject).OfType<PropertyMember>())
                    {
                        if (property.IsCompilerGenerated || property.IsStatic == true || property.Setter is null)
                        {
                            continue;
                        }

                        if (property.Writability == Writability.Writable)
                        {
                            violations.Add($"{valueObject.FullName} has writable property '{property.Name}'");
                        }
                    }
                }

                DcaRule.Fail("Value Object fields must be readonly for shallow immutability (Vernon's DDD).", violations);
            })
            .Selecting("Classes, record classes and structs below the root namespace assignable to IValue; enums excluded.")
        .Checking(
            "Instance fields, inherited ones included, are readonly; auto-property backing fields are inspected through their get-only or init-only property. Static state, referenced objects and collection contents are not inspected.");

    public static IDcaRule ValueObjectsHaveNoSetters() =>
        DcaRule.Check(
            "DCA-TAC-011",
            "Value Objects must not have setter methods",
            "Value Objects are immutable; state changes produce a new instance instead of mutating",
            arch =>
            {
                var violations = new List<string>();
                foreach (var valueObject in ConcreteTypesAssignableTo(arch, arch.Layout.Markers.Value))
                {
                    foreach (var setter in Setters(valueObject, publicOnly: false))
                    {
                        violations.Add($"{valueObject.FullName} has setter '{setter}'");
                    }
                }

                DcaRule.Fail("Value Objects must be immutable and should not have setter methods.", violations);
            })
            .Selecting(
                "Non-interface types below the root namespace assignable to IValue - records, "
                + "structs and enums included.")
            .Checking(
                "No member - inherited ones included - is a setter, regardless of visibility: "
                + "a property with a writable (non-init) set accessor, or a method named Set "
                + "followed by an upper-case letter with exactly one parameter and return type "
                + "void. A wither that returns a new instance is not a setter; a record's "
                + "init-only properties pass.");

    public static IDcaRule ValueObjectsAreRecordsOrHaveAttributeEquality() =>
        DcaRule.Check(
            "DCA-TAC-012",
            "Value Objects must be records or immutable classes with attribute equality",
            "A record grants attribute-based equality for free; a hand-written Value Object class must"
            + " override Equals and GetHashCode itself to compare by its attributes",
            arch =>
            {
                var violations = new List<string>();
                foreach (var valueObject in ConcreteTypesAssignableTo(arch, arch.Layout.Markers.Value))
                {
                    if (IsRecord(valueObject) || valueObject is Enum)
                    {
                        continue;
                    }

                    var runtime = arch.RuntimeType(valueObject);
                    var overridesEquality = runtime is not null
                        && OverridesOwn(runtime, nameof(Equals), typeof(object))
                        && OverridesOwn(runtime, nameof(GetHashCode));
                    if (!overridesEquality)
                    {
                        violations.Add($"{valueObject.FullName} is a non-record Value Object without its own Equals/GetHashCode");
                    }
                }

                DcaRule.Fail(
                    "Value Objects are records by preference; an immutable class is allowed, but it must implement attribute equality itself.",
                    violations);
            })
            .Selecting("Non-interface types below the root namespace assignable to IValue; enums excluded.")
        .Checking(
            "The runtime type or a base other than object or ValueType overrides Equals(object) and GetHashCode(). Record-generated equality counts; plain structs must supply both overrides.");

    // ---------------------------------------------------------------------------------------------
    // Repository pattern
    // ---------------------------------------------------------------------------------------------

    public static IDcaRule RepositoryInterfacesExtendMarker(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-TAC-013",
            "Repository Interfaces should extend the IRepository Marker Interface",
            "Repository interfaces should extend the IRepository marker interface",
            arch =>
            {
                var violations = new List<string>();
                foreach (var candidate in arch.Interfaces)
                {
                    if (!ResidesInAny(candidate, arch.AllApplicationPatterns())
                        || !HasSuffixButIsNotMarker(candidate, arch.Layout.RepositorySuffix))
                    {
                        continue;
                    }

                    if (!IsAssignableTo(arch, candidate, arch.Layout.Markers.Repository))
                    {
                        violations.Add($"{candidate.FullName} is named *Repository but does not extend {nameof(IRepository)}");
                    }
                }

                DcaRule.Fail("Repository interfaces must extend the IRepository marker.", violations);
            })
            .Selecting(
                "Interfaces in <module>.Application of every module root whose name ends with "
                + "Repository; interfaces named exactly Repository or IRepository excluded.")
            .Checking(
                "The interface is assignable to the IRepository marker. A *Repository "
                + "interface elsewhere is not selected; an empty selection passes.");

    public static IDcaRule RepositoryInterfacesResideInSharedOutputPorts(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-TAC-014",
            "Repository interfaces must reside in the application layer (local to a use case or shared)",
            "Repository interfaces are output ports in the application layer (Hexagonal Architecture)",
            arch => RequireNamespace(RepositoryInterfaces(arch), DcaLayout.AnyOf(arch.AllApplicationPatterns()), "Repository interfaces", "an application namespace of its module (use-case-local or Shared)"))
            .Selecting(
                "Interfaces below the root namespace assignable to IRepository, whatever "
                + "their name; an interface named exactly Repository excluded.")
            .Checking(
                "The interface resides in <module>.Application of some module root, the "
                + "shared kernel's included. Implementations are not selected; an empty "
                + "selection passes.");

    public static IDcaRule RepositoryImplementationsResideInOutgoingAdapters(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-TAC-015",
            "Repository Implementations must reside in the Adapter.Outgoing namespace",
            "Repository implementations are outgoing adapters in bounded contexts",
            arch => RequireNamespace(
                ConcreteTypesAssignableTo(arch, arch.Layout.Markers.Repository),
                DcaLayout.AnyOf(arch.AllOutgoingAdapterPatterns()), "Repository implementations", "the outgoing adapter namespace"))
            .Selecting(
                "Non-interface types below the root namespace assignable to IRepository, "
                + "abstract base classes included.")
            .Checking(
                "The type resides in <module>.Adapter.Outgoing of some module root. An "
                + "implementation anywhere else below the root - a test double, say - is "
                + "reported; an empty selection passes. An implementation that carries the "
                + "repository role and is also named *Repository is reported by DCA-HEX-008 as well: "
                + "that rule finds unmarked implementations by name, and the two populations overlap "
                + "where a project does both.");

    public static IDcaRule RepositoriesOnlyForAggregateRoots() =>
        DcaRule.Check(
            "DCA-TAC-016",
            "Repositories must only exist for Aggregate Roots",
            "A repository is the collection of one aggregate type; a repository for an entity would"
            + " let callers bypass the root that guards the aggregate's invariants",
            arch =>
            {
                var violations = new List<string>();
                foreach (var repository in RepositoryInterfaces(arch))
                {
                    var repoName = repository.Name.StartsWith("I", StringComparison.Ordinal) && repository.Name.Length > 1 && char.IsUpper(repository.Name[1])
                        ? repository.Name.Substring(1)
                        : repository.Name;
                    if (!repoName.EndsWith(arch.Layout.RepositorySuffix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var binding = BoundAggregateArgument(arch, repository);
                    if (binding is { } bound)
                    {
                        if (bound.IsGenericParameter)
                        {
                            // A generic intermediate port binds no aggregate of its own.
                            continue;
                        }

                        var boundType = arch.Types.FirstOrDefault(t => t.FullName == bound.FullName);
                        if (boundType is null || !IsAssignableTo(arch, boundType, arch.Layout.Markers.AggregateRoot))
                        {
                            violations.Add($"{repository.FullName} binds {bound.FullName} which does not implement {nameof(IAggregateRoot)}");
                        }
                        else if (repoName != bound.Name + arch.Layout.RepositorySuffix)
                        {
                            violations.Add(
                                $"{repository.FullName} binds {bound.FullName} but is named {repoName}"
                                + $" - name it {bound.Name}{arch.Layout.RepositorySuffix}");
                        }

                        continue;
                    }

                    var aggregateName = repoName.Substring(0, repoName.Length - arch.Layout.RepositorySuffix.Length);
                    var context = arch.RootContextNamespace(NamespaceOf(repository));
                    var candidates = NonInterfaceTypes(arch)
                        .Where(c => c.Name == aggregateName)
                        .Where(c => context is null || context == arch.RootContextNamespace(NamespaceOf(c)))
                        .ToList();
                    if (candidates.Count == 0)
                    {
                        violations.Add(
                            $"{repository.FullName} refers to '{aggregateName}' which cannot be resolved in its bounded context"
                            + $" ({context ?? "outside root namespace"}) - name the repository after the aggregate root it manages");
                        continue;
                    }

                    foreach (var candidate in candidates)
                    {
                        if (!IsAssignableTo(arch, candidate, arch.Layout.Markers.AggregateRoot))
                        {
                            violations.Add($"{repository.FullName} exists for {candidate.FullName} which does not implement {nameof(IAggregateRoot)}");
                        }
                    }
                }

                DcaRule.Fail(
                    "Repositories should only exist for Aggregate Roots, not for Entities (DDD pattern).",
                    violations,
                    "name the repository after the aggregate root it binds");
            })
            .Selecting(
                "Interfaces below the root namespace assignable to IRepository whose name "
                + "ends with Repository; an interface named exactly Repository excluded.")
            .Checking(
                "The aggregate is read from the type argument the interface binds: the first argument "
                + "of the generic repository role it implements. That type must be assignable to "
                + "IAggregateRoot, and the interface's name - minus a leading I followed by an "
                + "upper-case letter - must be that type's name plus 'Repository', so a repository "
                + "bound to one aggregate and named after another is reported. An unresolved type "
                + "argument is skipped: a generic intermediate port such as "
                + "IAuditedRepository<TAggregate, TId> binds no aggregate of its own. When the role is "
                + "not generic, is used raw, or the runtime type cannot be resolved, the aggregate is "
                + "resolved by name instead: among all non-interface types below the root with the "
                + "interface's name minus 'Repository' and in the same context - the nearest enclosing "
                + "namespace carrying a [BoundedContext] or [SharedKernel] marker class, falling back "
                + "to the first segment below the root namespace - at least one must exist and every "
                + "one must be assignable to IAggregateRoot. The interface's methods play no role.");

    public static IDcaRule RepositoriesReturnNoNonRootEntities() =>
        DcaRule.Check(
            "DCA-TAC-017",
            "Repository methods must not return non-root Entities",
            "A caller receiving an Entity that is not an Aggregate Root could mutate part of an"
            + " aggregate without passing its root, so the root's invariants would never run",
            arch =>
            {
                var violations = new List<string>();
                foreach (var repository in RepositoryInterfaces(arch))
                {
                    foreach (var method in repository.GetMethodMembers().Where(m => m.MethodForm == MethodForm.Normal))
                    {
                        foreach (var type in TypesInvolvedIn(method.ReturnTypeInstance))
                        {
                            if (IsConcreteNonRootEntity(arch, type) || (IsAssignableTo(arch, type, arch.Layout.Markers.Entity) && !IsAssignableTo(arch, type, arch.Layout.Markers.AggregateRoot)))
                            {
                                violations.Add($"{repository.FullName}.{SimpleName(method)} exposes {type.FullName}, an Entity that is not an Aggregate Root");
                            }
                        }
                    }
                }

                DcaRule.Fail(
                    "Repository methods must not expose an Entity that is not an Aggregate Root: a caller could mutate part of an aggregate without passing its root (DDD pattern).",
                    violations);
            })
            .Selecting(
                "Interfaces below the root namespace assignable to IRepository; an interface "
                + "named exactly Repository excluded.")
            .Checking(
                "No return type of a method declared on the interface itself - walked through "
                + "generic type arguments, so Task<T?>, IReadOnlyList<T> and "
                + "IAsyncEnumerable<T> are seen through - involves a type assignable to IEntity "
                + "that is not also assignable to IAggregateRoot. Inherited methods, property "
                + "accessors and parameter types are not inspected.");

    // ---------------------------------------------------------------------------------------------
    // Store pattern (Repository's sibling for non-aggregate operational data)
    // ---------------------------------------------------------------------------------------------

    public static IDcaRule StoreInterfacesExtendStoreMarker() =>
        DcaRule.Check(
            "DCA-TAC-018",
            "Store interfaces must extend the IStore marker, not IRepository",
            "Stores extend the IStore marker; IRepository is reserved for Aggregate Roots",
            arch =>
            {
                var violations = new List<string>();
                var adapterOrInfrastructure = DcaLayout.AnyOf(arch.AllAdapterPatterns().Concat(arch.AllInfrastructurePatterns()));
                foreach (var candidate in arch.Interfaces)
                {
                    if (!HasSuffixButIsNotMarker(candidate, arch.Layout.StoreSuffix)
                        || candidate.FullName == arch.Layout.Markers.Store
                        || (candidate.Namespace is not null
                            && Regex.IsMatch(candidate.Namespace.FullName, adapterOrInfrastructure)))
                    {
                        continue;
                    }

                    if (!IsAssignableTo(arch, candidate, arch.Layout.Markers.Store))
                    {
                        violations.Add($"{candidate.FullName} is named *Store but does not extend {nameof(IStore)}");
                    }

                    if (IsAssignableTo(arch, candidate, arch.Layout.Markers.Repository))
                    {
                        violations.Add($"{candidate.FullName} is named *Store but extends {nameof(IRepository)}");
                    }
                }

                DcaRule.Fail("Store interfaces must extend IStore and must not extend IRepository.", violations);
            })
            .Selecting(
                "Interfaces below the root namespace whose name ends with Store, those named exactly Store or"
                + " IStore and the type the Store role names excluded, outside every adapter and"
                + " infrastructure namespace - an interface there is an adapter's own abstraction, never a"
                + " port.")
            .Checking(
                "The interface is assignable to the IStore marker and not assignable to "
                + "IRepository; both must hold, and each failing alone is reported. An "
                + "interface not named *Store is never reported; an empty selection passes.");

    public static IDcaRule StoreInterfacesResideInSharedOutputPorts(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-TAC-019",
            "Store interfaces must reside in the application layer (local to a use case or shared)",
            "Store interfaces are output ports in the application layer (Hexagonal Architecture)",
            arch => RequireNamespace(StoreInterfaces(arch), DcaLayout.AnyOf(arch.AllApplicationPatterns()), "Store interfaces", "an application namespace of its module (use-case-local or Shared)"))
            .Selecting(
                "Interfaces below the root namespace assignable to IStore, whatever their "
                + "name; an interface named exactly Store excluded.")
            .Checking(
                "The interface resides in <module>.Application of some module root, the "
                + "shared kernel's included. Implementations are not selected; an empty "
                + "selection passes.");

    public static IDcaRule StoreImplementationsResideInOutgoingAdapters(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-TAC-020",
            "Store implementations must reside in the Adapter.Outgoing namespace",
            "Store implementations are outgoing adapters in bounded contexts",
            arch => RequireNamespace(
                ConcreteTypesAssignableTo(arch, arch.Layout.Markers.Store),
                DcaLayout.AnyOf(arch.AllOutgoingAdapterPatterns()), "Store implementations", "the outgoing adapter namespace"))
            .Selecting(
                "Non-interface types below the root namespace assignable to IStore, abstract "
                + "base classes included.")
            .Checking(
                "The type resides in <module>.Adapter.Outgoing of some module root; an empty "
                + "selection passes.");

    public static IDcaRule StoreInterfacesHaveNoRepositorySemantics() =>
        DcaRule.Check(
            "DCA-TAC-021",
            "Store interfaces must not declare Save or Delete methods",
            "Save/Delete are Repository semantics; a Store that has them is a Repository wearing the"
            + " wrong name, and the stored object should then be an Aggregate Root",
            arch =>
            {
                var violations = new List<string>();
                foreach (var store in StoreInterfaces(arch))
                {
                    foreach (var method in store.GetMethodMembers().Where(m => m.MethodForm == MethodForm.Normal))
                    {
                        var methodName = SimpleName(method);
                        if (RepositoryMethodNames.Contains(methodName))
                        {
                            violations.Add($"{store.FullName}.{methodName}() - Repository semantics on a Store");
                        }
                    }
                }

                DcaRule.Fail(
                    "Store interfaces use Record/Count/Exists semantics, not Save/Delete.",
                    violations,
                    "rename to *Repository if the stored object is an Aggregate Root, otherwise rename the methods to RecordAsync(...), CountAsync(...), ExistsAsync(...).");
            })
            .Selecting(
                "Interfaces below the root namespace assignable to IStore; an interface named "
                + "exactly Store excluded.")
            .Checking(
                "No method declared on the interface itself is named Save, DeleteById, Delete "
                + "or one of their asynchronous forms SaveAsync, DeleteByIdAsync, DeleteAsync - "
                + "matched by name alone, parameters and return type disregarded and case "
                + "ignored, so both languages match the same six names. Inherited methods are not "
                + "inspected, and no particular vocabulary (Record, Count, Exists) is required. "
                + "The six names are fixed and are not part of the marker roles: a vocabulary that "
                + "writes under another name is selected and then found to declare no write.");

    /// <summary>
    /// DCA-TAC-023. The aggregate's sibling of DCA-TAC-005: an entity is guarded by its root, the root guards
    /// itself by naming the ways it comes into being.
    /// </summary>
    public static IDcaRule AggregateRootsHaveNoPublicConstructors() =>
        DcaRule.Check(
            "DCA-TAC-023",
            "Aggregate Roots must not declare public constructors",
            "An aggregate comes into being in two ways - created as a business fact or reconstituted"
                + " from storage - and each is a named factory method. A public constructor is a"
                + " third, unnamed way that skips the creation invariants and the event creation"
                + " raises",
            arch =>
            {
                var violations = new List<string>();
                foreach (var root in AggregateRootClasses(arch))
                {
                    foreach (var constructor in PublicInstanceConstructors(arch, root))
                    {
                        violations.Add($"{root.FullName} declares public constructor {constructor}");
                    }
                }

                DcaRule.Fail(
                    "Aggregate Roots are created and reconstituted through named factory methods.",
                    violations,
                    "make the constructor private and add a static Create(...) for a new aggregate and a"
                        + " static Reconstitute(...) for the outgoing adapter that loads one.");
            })
            .Selecting(
                "Concrete, non-record classes below the root namespace assignable to IAggregateRoot."
                + " Abstract base classes are not selected - their constructors are reachable only from a"
                + " subclass. Records are not selected: a positional record's primary constructor is public"
                + " by the language, so a record root guards its invariants in that constructor instead."
                + " Structs are not selected either - a struct's parameterless constructor is always public.")
            .Checking(
                "No instance constructor declared by the class itself is public. Private, internal,"
                + " protected and private protected constructors pass - a persistence framework that needs"
                + " a parameterless constructor gets a non-public one. Static constructors are not"
                + " constructors in this sense. Who calls the factory methods is not checked. An empty"
                + " selection passes.");

    /// <summary>
    /// DCA-TAC-024 (experimental). One factory per origin: a neutral <c>Of</c> beside an origin factory that takes
    /// the same parameters leaves the caller to guess which checks run. Two named origin factories with equal
    /// parameters - from input, from storage - are the recommended shape and pass.
    /// </summary>
    public static IDcaRule ValueObjectsHaveNoNeutralFactoryBesideAnOriginFactory() =>
        DcaRule.Check(
            "DCA-TAC-024",
            "Value Objects must not declare a neutral factory beside an origin factory with the same parameters",
            "A value built from input and a value rebuilt from storage are two origins with different checks,"
                + " and each has its own named factory. A neutral factory beside them that takes the same"
                + " parameters leaves the caller to guess which checks run; a value with one origin keeps its"
                + " one factory",
            arch =>
            {
                var violations = new List<string>();
                foreach (var value in ConcreteTypesAssignableTo(arch, arch.Layout.Markers.Value)
                    .Where(t => ResidesInAny(t, DcaLayout.Below(arch.Layout.RootNamespace)))
                    .OrderBy(t => t.FullName, StringComparer.Ordinal))
                {
                    var runtime = arch.RuntimeType(value);
                    if (runtime is null || runtime.IsAbstract)
                    {
                        continue;
                    }

                    var factories = runtime
                        .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                        .Where(m => !m.IsSpecialName && !m.IsGenericMethodDefinition && m.ReturnType == runtime)
                        .ToList();
                    foreach (var neutral in factories.Where(m => m.Name == NeutralFactoryName))
                    {
                        var signature = Signature(neutral);
                        var beside = factories
                            .Where(m => m.Name != NeutralFactoryName && Signature(m) == signature)
                            .Select(m => m.Name)
                            .OrderBy(n => n, StringComparer.Ordinal)
                            .ToList();
                        if (beside.Count > 0)
                        {
                            violations.Add($"{value.FullName} declares the neutral factory {NeutralFactoryName}({signature})"
                                + $" beside {string.Join(", ", beside)} with the same parameters");
                        }
                    }
                }

                DcaRule.Fail(
                    "Value Objects must not declare a neutral factory beside an origin factory with the same parameters",
                    violations,
                    "remove the neutral factory and let each caller name the origin - the factory that validates input or the one that takes a stored value as given");
            })
            .Experimental()
            .Selecting(
                "Concrete classes, records and structs below the root namespace assignable to IValue, with"
                + " their runtime type in the loaded assemblies.")
            .Checking(
                "No public static method named Of, declared by the type itself and returning exactly that"
                + " type, sits beside another such method that takes the same parameter types in the same"
                + " sequence. Two named factories with equal parameters (FromInput, FromStored) pass, and so"
                + " does an Of that is the only factory for its parameters. Operators, generic methods,"
                + " inherited methods and factories that return a wrapper (a nullable struct, a task, a"
                + " result type) are not counted - a nullable reference return is the type itself and"
                + " counts; constructors are not factories in this sense. An empty selection passes.");

    /// <summary>The types a domain wraps in a value object instead of holding them bare.</summary>
    private static readonly HashSet<Type> BareTypes = new()
    {
        typeof(string), typeof(bool), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int),
        typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(char),
        typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly), typeof(TimeOnly), typeof(Guid),
    };

    /// <summary>The methods every type has from the platform, and the ones a record declares for itself.</summary>
    private static readonly HashSet<string> PlatformMethods = new(StringComparer.Ordinal)
    {
        "Equals", "GetHashCode", "ToString", "CompareTo", "Deconstruct", "PrintMembers", "<Clone>$",
    };

    /// <summary>
    /// DCA-TAC-025 (experimental). A string, a number, a flag or a moment that means something in the domain carries
    /// its rules in a value object of its own - a postal code, a quantity, a status. Held bare, its rules are repeated
    /// wherever it is used, or nowhere. Read by reflection over the declared members.
    /// </summary>
    public static IDcaRule DomainHoldsNoBarePrimitives() =>
        DcaRule.Check(
            "DCA-TAC-025",
            "Domain types must not hold bare primitives, strings or time types",
            "A value with rules of its own - a postal code, a quantity, a status, a moment - is a value object;"
                + " held as a bare string, int, bool or DateTimeOffset its validation is repeated at every use or"
                + " missing. A value object with several components holds value objects too; only a wrapper - a"
                + " value of exactly one component - holds the primitive it wraps",
            arch =>
            {
                var markers = arch.Layout.Markers;
                var blockNamespaces = new[]
                    {
                        markers.AggregateRoot, markers.Entity, markers.Value, markers.Id, markers.DomainEvent,
                        markers.IntegrationEvent, markers.DomainService, markers.Factory, markers.Specification,
                        markers.DomainException, markers.OutputPort, markers.Repository, markers.Store,
                    }
                    .Where(m => m.Contains('.'))
                    .Select(m => m[..m.LastIndexOf('.')])
                    .ToHashSet(StringComparer.Ordinal);
                var violations = new List<string>();
                foreach (var type in arch.Types
                    .Where(t => !t.IsCompilerGenerated && ResidesInAny(t, arch.AllDomainPatterns()))
                    .OrderBy(t => t.FullName, StringComparer.Ordinal))
                {
                    var runtime = arch.RuntimeType(type);
                    if (runtime is null || IsWrapperValue(runtime, markers))
                    {
                        continue;
                    }

                    var exempt = BuildingBlockMemberNames(runtime, blockNamespaces);
                    const BindingFlags declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                        | BindingFlags.Static | BindingFlags.DeclaredOnly;
                    var bare = new List<string>();
                    foreach (var field in runtime.GetFields(declared & ~BindingFlags.Static))
                    {
                        if (field.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute))
                            || runtime.IsEnum)
                        {
                            continue;
                        }

                        var found = BareTypesIn(field.FieldType);
                        if (found.Length > 0)
                        {
                            bare.Add($"field {field.Name} is {found}");
                        }
                    }

                    foreach (var property in runtime.GetProperties(declared & ~BindingFlags.Static))
                    {
                        if (exempt.Contains(property.Name) || property.GetIndexParameters().Length > 0
                            || property.Name == "EqualityContract")
                        {
                            continue;
                        }

                        var found = BareTypesIn(property.PropertyType);
                        if (found.Length > 0)
                        {
                            bare.Add($"property {property.Name} is {found}");
                        }
                    }

                    foreach (var method in runtime.GetMethods(declared))
                    {
                        if (method.IsSpecialName || PlatformMethods.Contains(method.Name) || exempt.Contains(method.Name)
                            || method.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)))
                        {
                            continue;
                        }

                        var predicate = method.ReturnType == typeof(bool) || method.ReturnType == typeof(bool?);
                        var returned = BareTypesIn(method.ReturnType);
                        if (returned.Length > 0 && !predicate)
                        {
                            bare.Add($"{method.Name}() returns {returned}");
                        }

                        var parameters = method.GetParameters();
                        for (var i = 0; i < parameters.Length; i++)
                        {
                            var taken = BareTypesIn(parameters[i].ParameterType);
                            if (taken.Length > 0)
                            {
                                bare.Add($"{method.Name}(...) takes {taken} as parameter {i + 1}");
                            }
                        }
                    }

                    if (bare.Count > 0)
                    {
                        violations.Add($"{type.FullName}: {string.Join("; ", bare.OrderBy(b => b, StringComparer.Ordinal))}");
                    }
                }

                DcaRule.Fail(
                    "Domain types must not hold bare primitives, strings or time types",
                    violations,
                    "wrap each in a value object that carries its rules - a value of one component may hold the primitive it wraps");
            })
            .Experimental()
            .Selecting(
                "Classes, records, structs, enums and interfaces in <Module>.Domain of every module root, the shared"
                + " kernel's domain included, with their runtime type in the loaded assemblies - their declared"
                + " instance fields and properties and their declared methods, private ones included; constructors"
                + " are not selected, the members they fill are. Not selected: a wrapper value - a type assignable"
                + " to IValue or IId with exactly one instance field - with everything it declares; compiler-"
                + " generated members (a property's backing field, a record's EqualityContract, Deconstruct,"
                + " PrintMembers); property accessors and operators; Equals, GetHashCode, ToString and CompareTo; an"
                + " enum's members; a member that implements a building block's, such as a domain event's OccurredOn"
                + " and EventId. A bool return is a question to the object - a predicate - and passes; a bool field,"
                + " property or parameter does not.")
            .Checking(
                "No selected field, property, return type or parameter type involves string, bool, a numeric"
                + " primitive or char, a point in time (DateTime, DateTimeOffset, DateOnly, TimeOnly), or a bare Guid -"
                + " an identifier says whose it is through a typed id - nullable"
                + " and generic arguments and arrays included, so IReadOnlyList<string> counts. decimal, TimeSpan"
                + " and the project's own types pass. A value object with several components is checked like"
                + " any other type: each component is a value object itself. An empty selection passes.");

    /// <summary>A value or an identifier of exactly one instance field: it wraps that field's primitive.</summary>
    private static bool IsWrapperValue(Type runtime, DcaMarkers markers) =>
        (DcaMarkers.IsAssignableToByName(runtime, markers.Value) || DcaMarkers.IsAssignableToByName(runtime, markers.Id))
        && runtime.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Length == 1;

    /// <summary>The names of the members a building block the type implements or derives from declares.</summary>
    private static HashSet<string> BuildingBlockMemberNames(Type runtime, HashSet<string> blockNamespaces)
    {
        var supertypes = runtime.GetInterfaces().AsEnumerable();
        for (var b = runtime.BaseType; b is not null; b = b.BaseType)
        {
            supertypes = supertypes.Append(b);
        }

        return supertypes
            .Where(s => s.Namespace is not null && blockNamespaces.Contains(s.Namespace))
            .SelectMany(s => s.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>The bare types a declared type involves - nullable, array and generic arguments included; "" when none.</summary>
    private static string BareTypesIn(Type type)
    {
        var found = new SortedSet<string>(StringComparer.Ordinal);
        var pending = new Stack<Type>();
        pending.Push(type);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (current.IsByRef || current.IsArray || current.IsPointer)
            {
                pending.Push(current.GetElementType()!);
                continue;
            }

            if (BareTypes.Contains(current))
            {
                found.Add(current.Name);
            }

            if (current.IsGenericType)
            {
                foreach (var argument in current.GetGenericArguments())
                {
                    pending.Push(argument);
                }
            }
        }

        return string.Join(", ", found);
    }

    /// <summary>The neutral factory name, as C# spells it.</summary>
    private const string NeutralFactoryName = "Of";

    private static string Signature(MethodInfo method) =>
        string.Join(", ", method.GetParameters().Select(p => p.ParameterType.FullName ?? p.ParameterType.Name));

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    /// <summary>Loaded classes, structs and enums (no interfaces, no compiler-generated types).</summary>
    private static IEnumerable<IType> NonInterfaceTypes(DcaArchitecture arch) =>
        arch.Types.Where(t => t is not Interface && !t.IsCompilerGenerated && !t.IsGenericParameter);

    private static IEnumerable<IType> ConcreteTypesAssignableTo(DcaArchitecture arch, string marker) =>
        NonInterfaceTypes(arch).Where(t => IsAssignableTo(arch, t, marker));

    /// <summary>Concrete, non-record aggregate root classes (no interfaces, structs, abstract bases or records).</summary>
    private static IEnumerable<Class> AggregateRootClasses(DcaArchitecture arch) =>
        NonInterfaceTypes(arch).OfType<Class>()
            .Where(c => c.IsAbstract != true && !IsRecord(c) && IsAssignableTo(arch, c, arch.Layout.Markers.AggregateRoot));

    /// <summary>
    /// The public instance constructors a class declares itself, read by reflection where the runtime type is
    /// available (static constructors excluded) and from the ArchUnitNET members otherwise.
    /// </summary>
    private static IEnumerable<string> PublicInstanceConstructors(DcaArchitecture arch, Class type)
    {
        var runtime = arch.RuntimeType(type);
        if (runtime is not null)
        {
            return runtime
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(c => $"{runtime.Name}({string.Join(", ", c.GetParameters().Select(p => p.ParameterType.Name))})")
                .ToList();
        }

        return type.GetMethodMembers()
            .Where(m => m.MethodForm == MethodForm.Constructor && m.IsStatic != true && m.Visibility == Visibility.Public)
            .Select(m => m.FullName)
            .ToList();
    }

    private static IEnumerable<IType> NonRootEntities(DcaArchitecture arch) =>
        ConcreteTypesAssignableTo(arch, arch.Layout.Markers.Entity).Where(t => !IsAssignableTo(arch, t, arch.Layout.Markers.AggregateRoot));

    /// <summary>
    /// The aggregate type a repository interface binds: the first type argument of the generic
    /// repository role it implements. Null when the role is not generic, is used raw, or when the
    /// runtime type cannot be resolved — the rule then falls back to resolving the aggregate by name.
    /// </summary>
    private static Type? BoundAggregateArgument(DcaArchitecture arch, IType repository)
    {
        var runtime = arch.RuntimeType(repository);
        if (runtime is null)
        {
            return null;
        }

        foreach (var candidate in runtime.GetInterfaces())
        {
            if (candidate.IsGenericType
                && DcaMarkers.CarriesRole(candidate.GetGenericTypeDefinition(), arch.Layout.Markers.Repository))
            {
                var arguments = candidate.GetGenericArguments();
                if (arguments.Length > 0)
                {
                    return arguments[0];
                }
            }
        }

        return null;
    }

    private static IEnumerable<Interface> RepositoryInterfaces(DcaArchitecture arch) =>
        arch.Interfaces.Where(i => i.Name != arch.Layout.RepositorySuffix && IsAssignableTo(arch, i, arch.Layout.Markers.Repository));

    private static IEnumerable<Interface> StoreInterfaces(DcaArchitecture arch) =>
        arch.Interfaces.Where(i => i.Name != arch.Layout.StoreSuffix && IsAssignableTo(arch, i, arch.Layout.Markers.Store));

    private static bool HasSuffixButIsNotMarker(IType type, string suffix) =>
        type.Name.EndsWith(suffix, StringComparison.Ordinal) && type.Name != suffix && type.Name != "I" + suffix;

    private static bool IsConcreteAggregateRoot(DcaArchitecture arch, IType type) =>
        IsAssignableTo(arch, type, arch.Layout.Markers.AggregateRoot);

    private static bool IsConcreteNonRootEntity(DcaArchitecture arch, IType type) =>
        IsAssignableTo(arch, type, arch.Layout.Markers.Entity) && !IsAssignableTo(arch, type, arch.Layout.Markers.AggregateRoot);

    /// <summary>A record class, a struct (record structs are structs) — the .NET reading of Java's "record".</summary>
    internal static bool IsRecordLike(IType type) => type is Struct || type is Class { IsRecord: true };

    internal static bool IsRecord(IType type) => type is Class { IsRecord: true }
        || type is Struct && type.Members.OfType<MethodMember>().Any(m =>
            SimpleName(m) == "PrintMembers" && m.IsCompilerGenerated);

    /// <summary>Shallow shape only: inherited state is inspected, referenced contents are not.</summary>
    internal static bool IsImmutableShape(IType type) =>
        (type is Struct || type is Class { IsRecord: true } || type is Class { IsSealed: true })
        && AllMembers(type).OfType<FieldMember>().Where(f => f.IsStatic != true
            && !f.Name.Contains("k__BackingField", StringComparison.Ordinal))
            .All(f => f.Writability == Writability.ReadOnly)
        && AllMembers(type).OfType<PropertyMember>().Where(p => p.IsStatic != true)
            .All(p => p.Setter is null || p.Writability != Writability.Writable)
        && !AllMembers(type).OfType<MethodMember>().Any(m => m.IsStatic != true
            && m.MethodForm == MethodForm.Normal && SimpleName(m).StartsWith("Set", StringComparison.Ordinal)
            && m.Parameters.Any() && m.ReturnType.FullName == typeof(void).FullName);

    private static string NamespaceOf(IType type) => type.Namespace?.FullName ?? string.Empty;

    private static bool ResidesInAny(IType type, params string[] patterns) =>
        patterns.Any(p => Regex.IsMatch(NamespaceOf(type), p));

    /// <summary>
    /// Assignability by full name of the (non-generic) marker. ArchUnitNET's own check is tried first;
    /// when the type is available at runtime, reflection decides, which also sees interfaces that a
    /// base class from another assembly implements.
    /// </summary>
    private static bool IsAssignableTo(DcaArchitecture arch, IType type, string marker)
    {
        if (type.IsGenericParameter)
        {
            return false;
        }

        var runtime = arch.RuntimeType(type);
        if (runtime is not null)
        {
            return DcaMarkers.IsAssignableToByName(runtime, marker);
        }

        return type.IsAssignableTo(marker) || type.ImplementsInterface(marker);
    }

    private static IEnumerable<IMember> AllMembers(IType type) =>
        type is Class cls ? cls.MembersIncludingInherited : type.Members;

    /// <summary>
    /// Instance fields and properties of a type (inherited included), skipping compiler-generated and record
    /// plumbing. <see cref="DataMember.ElementTypes"/> holds every type the member's type involves besides itself:
    /// generic arguments from the ArchUnitNET model and, when the runtime type is available, what reflection sees
    /// in the inspected type's context — array element types, generic arguments recursively, and the concrete type a
    /// generic base class's parameter is bound to (<c>class Base&lt;T&gt; { T Value; }</c> read from
    /// <c>Order : Base&lt;Customer&gt;</c> involves <c>Customer</c>).
    /// </summary>
    private static IEnumerable<DataMember> DataMembers(DcaArchitecture arch, IType type)
    {
        var runtime = arch.RuntimeType(type);
        foreach (var member in AllMembers(type))
        {
            switch (member)
            {
                case FieldMember field when !field.IsCompilerGenerated && !field.Name.Contains("k__BackingField", StringComparison.Ordinal):
                    yield return new DataMember(field.Name, field.Type, ElementTypes(arch, runtime, field.Name, field.Type, field.GenericArguments), field.IsStatic == true);
                    break;
                case PropertyMember property when !property.IsCompilerGenerated && property.Name != "EqualityContract":
                    yield return new DataMember(property.Name, property.Type, ElementTypes(arch, runtime, property.Name, property.Type, property.GenericArguments), property.IsStatic == true);
                    break;
            }
        }
    }

    /// <summary>The instance members of <see cref="DataMembers"/>: static fields and properties carry no aggregate state.</summary>
    private static IEnumerable<DataMember> InstanceDataMembers(DcaArchitecture arch, IType type) =>
        DataMembers(arch, type).Where(m => !m.IsStatic);

    private static IReadOnlyList<IType> ElementTypes(DcaArchitecture arch, Type? runtime, string memberName, IType memberType, IEnumerable<GenericArgument> genericArguments)
    {
        var elements = new List<IType>(genericArguments.SelectMany(TypesInvolvedIn));
        var runtimeMemberType = runtime is null ? null : RuntimeMemberType(runtime, memberName);
        if (runtimeMemberType is not null)
        {
            // ArchUnitNET models an array member by its element type, so the element is an *element* here only
            // when the member really is an array or a generic instantiation - never for a plain member.
            var plain = runtimeMemberType is { IsArray: false, IsGenericType: false };
            foreach (var involved in InvolvedRuntimeTypes(runtimeMemberType))
            {
                var modelled = arch.Types.FirstOrDefault(t => t.FullName == involved.FullName);
                if (modelled is not null && !(plain && modelled.Equals(memberType)) && !elements.Contains(modelled))
                {
                    elements.Add(modelled);
                }
            }
        }

        return elements;
    }

    /// <summary>The declared type of the named instance field or property, as reflection sees it on the inspected type.</summary>
    private static Type? RuntimeMemberType(Type runtime, string memberName)
    {
        const BindingFlags own = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        for (var t = runtime; t is not null && t != typeof(object); t = t.BaseType)
        {
            var property = t.GetProperty(memberName, own);
            if (property is not null)
            {
                return property.PropertyType;
            }

            var field = t.GetField(memberName, own);
            if (field is not null)
            {
                return field.FieldType;
            }
        }

        return null;
    }

    /// <summary>A type, its array element types and its generic arguments, recursively; open type parameters excluded.</summary>
    private static IEnumerable<Type> InvolvedRuntimeTypes(Type type)
    {
        if (type.IsGenericParameter)
        {
            yield break;
        }

        if (type.IsArray)
        {
            foreach (var involved in InvolvedRuntimeTypes(type.GetElementType()!))
            {
                yield return involved;
            }

            yield break;
        }

        yield return type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                foreach (var involved in InvolvedRuntimeTypes(argument))
                {
                    yield return involved;
                }
            }
        }
    }

    private static IEnumerable<Type> RuntimeDataMemberTypes(DcaArchitecture arch, IType type)
    {
        var runtime = arch.RuntimeType(type);
        if (runtime is null)
        {
            return Enumerable.Empty<Type>();
        }

        const BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        return runtime.GetProperties(all).Select(p => p.PropertyType)
            .Concat(runtime.GetFields(all).Select(f => f.FieldType))
            .Concat(BaseTypes(runtime).SelectMany(b => b.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Select(f => f.FieldType)));
    }

    private static IEnumerable<Type> BaseTypes(Type type)
    {
        for (var b = type.BaseType; b is not null && b != typeof(object); b = b.BaseType)
        {
            yield return b;
        }
    }

    /// <summary>Every type involved in a type instance, including generic arguments recursively (Task&lt;T?&gt;, IReadOnlyList&lt;T&gt;, …).</summary>
    private static IEnumerable<IType> TypesInvolvedIn(ITypeInstance<IType> instance)
    {
        yield return instance.Type;
        foreach (var argument in instance.GenericArguments)
        {
            foreach (var involved in TypesInvolvedIn(argument))
            {
                yield return involved;
            }
        }
    }

    private static IEnumerable<IType> TypesInvolvedIn(GenericArgument argument)
    {
        yield return argument.Type;
        foreach (var nested in argument.GenericArguments)
        {
            foreach (var involved in TypesInvolvedIn(nested))
            {
                yield return involved;
            }
        }
    }

    /// <summary>
    /// Setter-like members: properties with a settable (non-init) accessor and methods named
    /// <c>Set*</c> with one parameter returning void. Record plumbing and compiler-generated members are skipped.
    /// </summary>
    private static IEnumerable<string> Setters(IType type, bool publicOnly)
    {
        foreach (var member in AllMembers(type))
        {
            switch (member)
            {
                case PropertyMember property when !property.IsCompilerGenerated && property.Setter is not null && property.Writability == Writability.Writable:
                    if (!publicOnly || property.SetterVisibility == Visibility.Public)
                    {
                        yield return property.Name;
                    }

                    break;
                case MethodMember method when method.MethodForm == MethodForm.Normal && !method.IsCompilerGenerated && IsSetterMethod(method):
                    if (!publicOnly || method.Visibility == Visibility.Public)
                    {
                        yield return SimpleName(method);
                    }

                    break;
            }
        }
    }

    /// <summary>ArchUnitNET member names carry the parameter list (<c>SaveAsync(System.String)</c>); this strips it.</summary>
    private static string SimpleName(MethodMember method)
    {
        var paren = method.Name.IndexOf('(', StringComparison.Ordinal);
        return paren < 0 ? method.Name : method.Name.Substring(0, paren);
    }

    private static bool IsSetterMethod(MethodMember method) =>
        SimpleName(method) is { Length: > 3 } name
        && name.StartsWith("Set", StringComparison.Ordinal)
        && char.IsUpper(name[3])
        && method.Parameters.Count() == 1
        && method.ReturnType.FullName == typeof(void).FullName;

    private static bool OverridesOwn(Type type, string methodName, params Type[] parameterTypes)
    {
        var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameterTypes, null);
        return method is not null && method.DeclaringType != typeof(object) && method.DeclaringType != typeof(ValueType);
    }

    private static void RequireNamespace(IEnumerable<IType> types, string pattern, string subject, string location)
    {
        var violations = types
            .Where(t => !Regex.IsMatch(NamespaceOf(t), pattern))
            .Select(t => $"{t.FullName} resides in {NamespaceOf(t)}")
            .ToList();
        DcaRule.Fail($"{subject} must reside in {location}.", violations);
    }

    private static string FieldDescription(IType owner, DataMember member) =>
        $"{owner.FullName} has field '{member.Name}' of type {member.Type.FullName}";

    private static string ContainsDescription(IType owner, DataMember member, IType element) =>
        $"{owner.FullName} has field '{member.Name}' containing {element.FullName}";

    private sealed record DataMember(string Name, IType Type, IReadOnlyList<IType> ElementTypes, bool IsStatic = false);
}
