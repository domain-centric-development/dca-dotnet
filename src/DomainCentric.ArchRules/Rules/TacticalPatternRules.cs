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
    private const string RepositorySuffix = "Repository";
    private const string StoreSuffix = "Store";

    private static readonly HashSet<string> RepositoryMethodNames = new(StringComparer.Ordinal)
    {
        "FindByIdAsync", "SaveAsync", "DeleteByIdAsync", "DeleteAsync",
        "FindById", "Save", "DeleteById", "Delete",
        "findById", "save", "deleteById", "delete",
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
            EnrichedModelsAreValueRecords(layout),
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
                    if (!type.Name.EndsWith("AggregateRoot", StringComparison.Ordinal)
                        || type.Name == "AggregateRoot"
                        || !ResidesInAny(type, layout.DomainModelPattern, layout.SharedKernelDomainPattern))
                    {
                        continue;
                    }

                    if (!IsAssignableTo(arch, type, typeof(IAggregateRoot)))
                    {
                        violations.Add($"{type.FullName} is named *AggregateRoot but does not implement {nameof(IAggregateRoot)}");
                    }
                }

                DcaRule.Fail("Classes named *AggregateRoot must implement the IAggregateRoot marker.", violations);
            });

    public static IDcaRule AggregateRootsHoldNoOutputPorts() =>
        DcaRule.Check(
            "DCA-TAC-002",
            "Aggregate Roots must not hold references to Repositories or other Output Ports",
            "Aggregates are persistence-ignorant: repositories and services are passed as method"
            + " parameters by the use case, never injected as fields",
            arch =>
            {
                var violations = new List<string>();
                foreach (var aggregate in ConcreteTypesAssignableTo(arch, typeof(IAggregateRoot)))
                {
                    foreach (var member in DataMembers(aggregate))
                    {
                        var fieldType = member.Type;
                        if (IsAssignableTo(arch, fieldType, typeof(IRepository)) || IsAssignableTo(arch, fieldType, typeof(IOutputPort)))
                        {
                            violations.Add($"{FieldDescription(aggregate, member)} which is a repository/output port");
                        }
                    }
                }

                DcaRule.Fail(
                    "Aggregates must not have injected repositories or output ports - pass dependencies as method parameters.",
                    violations);
            });

    public static IDcaRule AggregateRootsReferenceOtherAggregatesById() =>
        DcaRule.Check(
            "DCA-TAC-003",
            "Aggregate Roots must not have fields with other Aggregate Root types",
            "Vernon's Aggregate Design Rule #2: reference other Aggregates by identity to keep"
            + " aggregate boundaries and transactional consistency intact",
            arch =>
            {
                var violations = new List<string>();
                foreach (var aggregate in ConcreteTypesAssignableTo(arch, typeof(IAggregateRoot)))
                {
                    foreach (var member in DataMembers(aggregate))
                    {
                        if (IsConcreteAggregateRoot(arch, member.Type) && !member.Type.Equals(aggregate))
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

                DcaRule.Fail("Aggregates must reference other aggregates by ID only (Vernon's Rule #2).", violations);
            });

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
                foreach (var entity in ConcreteTypesAssignableTo(arch, typeof(IEntity)))
                {
                    if (entity is Class { IsAbstract: true })
                    {
                        continue;
                    }

                    var hasIdMember = DataMembers(entity).Any(m => IsAssignableTo(arch, m.Type, typeof(IId)))
                        || RuntimeDataMemberTypes(arch, entity).Any(t => typeof(IId).IsAssignableFrom(t));
                    if (!hasIdMember)
                    {
                        violations.Add($"{entity.FullName} has no field or property whose type implements {nameof(IId)}");
                    }
                }

                DcaRule.Fail("Entities must have an identity field typed as an IId value object (DDD pattern).", violations);
            });

    public static IDcaRule EntitiesHaveNoPublicConstructors() =>
        DcaRule.Check(
            "DCA-TAC-005",
            "Entities must not be instantiated directly from outside the aggregate",
            "Entities are created through their aggregate root so that the root can enforce its invariants",
            arch =>
            {
                var violations = new List<string>();
                foreach (var entity in NonRootEntities(arch))
                {
                    if (IsRecordLike(entity))
                    {
                        continue;
                    }

                    var publicConstructors = entity.GetConstructors().Count(c => c.Visibility == Visibility.Public);
                    for (var i = 0; i < publicConstructors; i++)
                    {
                        violations.Add($"{entity.FullName} has public constructor - should be internal, private or protected");
                    }
                }

                DcaRule.Fail(
                    "Entities should not have public constructors (access only through aggregate root).\nNote: Records are excluded from this rule.",
                    violations);
            });

    public static IDcaRule DomainModelHasNoPublicSetters() =>
        DcaRule.Check(
            "DCA-TAC-006",
            "Domain model classes must not have public setter methods",
            "Behavior-rich domain models change state through intention-revealing methods from the"
            + " ubiquitous language, never through public property setters",
            arch =>
            {
                var violations = new List<string>();
                foreach (var domainClass in ConcreteTypesAssignableTo(arch, typeof(IEntity)))
                {
                    foreach (var setter in Setters(domainClass, publicOnly: true))
                    {
                        violations.Add($"{domainClass.FullName} has public setter '{setter}'");
                    }
                }

                DcaRule.Fail(
                    "Domain model classes must not expose public setters - use intention-revealing methods from the ubiquitous language.",
                    violations);
            });

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
                    foreach (var member in DataMembers(entity))
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
            });

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
                foreach (var valueObject in ConcreteTypesAssignableTo(arch, typeof(IValue)))
                {
                    foreach (var member in DataMembers(valueObject))
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
            });

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
                        || !ResidesInAny(valueObject, layout.DomainModelPattern, layout.SharedKernelDomainPattern)
                        || !IsAssignableTo(arch, valueObject, typeof(IValue)))
                    {
                        continue;
                    }

                    if (valueObject.IsSealed != true && valueObject.IsAbstract != true)
                    {
                        violations.Add($"{valueObject.FullName} is a Value Object {(valueObject.IsRecord == true ? "record" : "class")} that is not sealed");
                    }
                }

                DcaRule.Fail("Value Object classes and records must be sealed (or be structs).", violations);
            });

    public static IDcaRule ValueObjectFieldsAreFinal() =>
        DcaRule.Check(
            "DCA-TAC-010",
            "Value Object fields must be readonly (deep immutability)",
            "Records have implicitly init-only state and enums are immutable by design; a hand-written"
            + " value class must make every instance field readonly and every property get-only or init-only itself",
            arch =>
            {
                var violations = new List<string>();
                foreach (var valueObject in ConcreteTypesAssignableTo(arch, typeof(IValue)))
                {
                    if (IsRecordLike(valueObject) || valueObject is Enum)
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

                DcaRule.Fail("Value Object fields must be readonly for deep immutability (Vernon's DDD).", violations);
            });

    public static IDcaRule ValueObjectsHaveNoSetters() =>
        DcaRule.Check(
            "DCA-TAC-011",
            "Value Objects must not have setter methods",
            "Value Objects are immutable; state changes produce a new instance instead of mutating",
            arch =>
            {
                var violations = new List<string>();
                foreach (var valueObject in ConcreteTypesAssignableTo(arch, typeof(IValue)))
                {
                    foreach (var setter in Setters(valueObject, publicOnly: false))
                    {
                        violations.Add($"{valueObject.FullName} has setter '{setter}'");
                    }
                }

                DcaRule.Fail("Value Objects must be immutable and should not have setter methods.", violations);
            });

    public static IDcaRule ValueObjectsAreRecordsOrHaveAttributeEquality() =>
        DcaRule.Check(
            "DCA-TAC-012",
            "Value Objects must be records or immutable classes with attribute equality",
            "A record grants attribute-based equality for free; a hand-written Value Object class must"
            + " override Equals and GetHashCode itself to compare by its attributes",
            arch =>
            {
                var violations = new List<string>();
                foreach (var valueObject in ConcreteTypesAssignableTo(arch, typeof(IValue)))
                {
                    if (IsRecordLike(valueObject) || valueObject is Enum)
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
            });

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
                    if (!Regex.IsMatch(NamespaceOf(candidate), layout.ApplicationPattern)
                        || !HasSuffixButIsNotMarker(candidate, RepositorySuffix))
                    {
                        continue;
                    }

                    if (!IsAssignableTo(arch, candidate, typeof(IRepository)))
                    {
                        violations.Add($"{candidate.FullName} is named *Repository but does not extend {nameof(IRepository)}");
                    }
                }

                DcaRule.Fail("Repository interfaces must extend the IRepository marker.", violations);
            });

    public static IDcaRule RepositoryInterfacesResideInSharedOutputPorts(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-TAC-014",
            "Repository interfaces must reside in the application layer's shared output-port namespace",
            "Repository interfaces are output ports in the application layer (Hexagonal Architecture)",
            arch => RequireNamespace(RepositoryInterfaces(arch), layout.SharedOutputPortPattern, "Repository interfaces", "the application layer's Shared namespace"));

    public static IDcaRule RepositoryImplementationsResideInOutgoingAdapters(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-TAC-015",
            "Repository Implementations must reside in the Adapter.Outgoing namespace",
            "Repository implementations are outgoing adapters in bounded contexts",
            arch => RequireNamespace(
                ConcreteTypesAssignableTo(arch, typeof(IRepository)),
                layout.OutgoingAdapterPattern, "Repository implementations", "the outgoing adapter namespace"));

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
                    if (!repoName.EndsWith(RepositorySuffix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var aggregateName = repoName.Substring(0, repoName.Length - RepositorySuffix.Length);
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
                        if (!IsAssignableTo(arch, candidate, typeof(IAggregateRoot)))
                        {
                            violations.Add($"{repository.FullName} exists for {candidate.FullName} which does not implement {nameof(IAggregateRoot)}");
                        }
                    }
                }

                DcaRule.Fail("Repositories should only exist for Aggregate Roots, not for Entities (DDD pattern).", violations);
            });

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
                            if (IsConcreteNonRootEntity(arch, type) || (IsAssignableTo(arch, type, typeof(IEntity)) && !IsAssignableTo(arch, type, typeof(IAggregateRoot))))
                            {
                                violations.Add($"{repository.FullName}.{SimpleName(method)} exposes {type.FullName}, an Entity that is not an Aggregate Root");
                            }
                        }
                    }
                }

                DcaRule.Fail(
                    "Repository methods must not expose an Entity that is not an Aggregate Root: a caller could mutate part of an aggregate without passing its root (DDD pattern).",
                    violations);
            });

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
                foreach (var candidate in arch.Interfaces)
                {
                    if (!HasSuffixButIsNotMarker(candidate, StoreSuffix))
                    {
                        continue;
                    }

                    if (!IsAssignableTo(arch, candidate, typeof(IStore)))
                    {
                        violations.Add($"{candidate.FullName} is named *Store but does not extend {nameof(IStore)}");
                    }

                    if (IsAssignableTo(arch, candidate, typeof(IRepository)))
                    {
                        violations.Add($"{candidate.FullName} is named *Store but extends {nameof(IRepository)}");
                    }
                }

                DcaRule.Fail("Store interfaces must extend IStore and must not extend IRepository.", violations);
            });

    public static IDcaRule StoreInterfacesResideInSharedOutputPorts(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-TAC-019",
            "Store interfaces must reside in the application layer's shared output-port namespace",
            "Store interfaces are output ports in the application layer (Hexagonal Architecture)",
            arch => RequireNamespace(StoreInterfaces(arch), layout.SharedOutputPortPattern, "Store interfaces", "the application layer's Shared namespace"));

    public static IDcaRule StoreImplementationsResideInOutgoingAdapters(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-TAC-020",
            "Store implementations must reside in the Adapter.Outgoing namespace",
            "Store implementations are outgoing adapters in bounded contexts",
            arch => RequireNamespace(
                ConcreteTypesAssignableTo(arch, typeof(IStore)),
                layout.OutgoingAdapterPattern, "Store implementations", "the outgoing adapter namespace"));

    public static IDcaRule StoreInterfacesHaveNoRepositorySemantics() =>
        DcaRule.Check(
            "DCA-TAC-021",
            "Store interfaces must not declare FindById or Save methods",
            "FindById/Save are Repository semantics; a Store that has them is a Repository wearing the"
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
                    "Store interfaces use Record/Count/Exists semantics, not FindById/Save.",
                    violations,
                    "rename to *Repository if the stored object is an Aggregate Root, otherwise rename the methods to RecordAsync(...), CountAsync(...), ExistsAsync(...).");
            });

    // ---------------------------------------------------------------------------------------------
    // Enriched domain model pattern
    // ---------------------------------------------------------------------------------------------

    public static IDcaRule EnrichedModelsAreValueRecords(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-TAC-022",
            "Enriched Domain Models must be Value Object records",
            "Enriched domain models are immutable read projections and must be records implementing IValue",
            arch =>
            {
                var violations = new List<string>();
                foreach (var type in NonInterfaceTypes(arch))
                {
                    if (!type.Name.StartsWith("Enriched", StringComparison.Ordinal)
                        || !Regex.IsMatch(NamespaceOf(type), layout.DomainModelPattern)
                        || IsAssignableTo(arch, type, typeof(IFactory)))
                    {
                        continue;
                    }

                    if (!IsRecordLike(type))
                    {
                        violations.Add($"{type.FullName} is not a record");
                    }

                    if (!IsAssignableTo(arch, type, typeof(IValue)))
                    {
                        violations.Add($"{type.FullName} does not implement {nameof(IValue)}");
                    }
                }

                DcaRule.Fail("Enriched domain models must be records implementing IValue.", violations);
            });

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    /// <summary>Loaded classes, structs and enums (no interfaces, no compiler-generated types).</summary>
    private static IEnumerable<IType> NonInterfaceTypes(DcaArchitecture arch) =>
        arch.Types.Where(t => t is not Interface && !t.IsCompilerGenerated && !t.IsGenericParameter);

    private static IEnumerable<IType> ConcreteTypesAssignableTo(DcaArchitecture arch, Type marker) =>
        NonInterfaceTypes(arch).Where(t => IsAssignableTo(arch, t, marker));

    private static IEnumerable<IType> NonRootEntities(DcaArchitecture arch) =>
        ConcreteTypesAssignableTo(arch, typeof(IEntity)).Where(t => !IsAssignableTo(arch, t, typeof(IAggregateRoot)));

    private static IEnumerable<Interface> RepositoryInterfaces(DcaArchitecture arch) =>
        arch.Interfaces.Where(i => i.Name != RepositorySuffix && IsAssignableTo(arch, i, typeof(IRepository)));

    private static IEnumerable<Interface> StoreInterfaces(DcaArchitecture arch) =>
        arch.Interfaces.Where(i => i.Name != StoreSuffix && IsAssignableTo(arch, i, typeof(IStore)));

    private static bool HasSuffixButIsNotMarker(IType type, string suffix) =>
        type.Name.EndsWith(suffix, StringComparison.Ordinal) && type.Name != suffix && type.Name != "I" + suffix;

    private static bool IsConcreteAggregateRoot(DcaArchitecture arch, IType type) =>
        type is not Interface && IsAssignableTo(arch, type, typeof(IAggregateRoot));

    private static bool IsConcreteNonRootEntity(DcaArchitecture arch, IType type) =>
        type is not Interface && IsAssignableTo(arch, type, typeof(IEntity)) && !IsAssignableTo(arch, type, typeof(IAggregateRoot));

    /// <summary>A record class, a struct (record structs are structs) — the .NET reading of Java's "record".</summary>
    private static bool IsRecordLike(IType type) => type is Struct || type is Class { IsRecord: true };

    private static string NamespaceOf(IType type) => type.Namespace?.FullName ?? string.Empty;

    private static bool ResidesInAny(IType type, params string[] patterns) =>
        patterns.Any(p => Regex.IsMatch(NamespaceOf(type), p));

    /// <summary>
    /// Assignability by full name of the (non-generic) marker. ArchUnitNET's own check is tried first;
    /// when the type is available at runtime, reflection decides, which also sees interfaces that a
    /// base class from another assembly implements.
    /// </summary>
    private static bool IsAssignableTo(DcaArchitecture arch, IType type, Type marker)
    {
        if (type.IsGenericParameter)
        {
            return false;
        }

        var runtime = arch.RuntimeType(type);
        if (runtime is not null)
        {
            return marker.IsAssignableFrom(runtime);
        }

        return type.IsAssignableTo(marker.FullName!) || type.ImplementsInterface(marker.FullName!);
    }

    private static IEnumerable<IMember> AllMembers(IType type) =>
        type is Class cls ? cls.MembersIncludingInherited : type.Members;

    /// <summary>Instance fields and properties of a type (inherited included), skipping compiler-generated and record plumbing.</summary>
    private static IEnumerable<DataMember> DataMembers(IType type)
    {
        foreach (var member in AllMembers(type))
        {
            switch (member)
            {
                case FieldMember field when !field.IsCompilerGenerated && !field.Name.Contains("k__BackingField", StringComparison.Ordinal):
                    yield return new DataMember(field.Name, field.Type, field.GenericArguments.SelectMany(TypesInvolvedIn).ToList());
                    break;
                case PropertyMember property when !property.IsCompilerGenerated && property.Name != "EqualityContract":
                    yield return new DataMember(property.Name, property.Type, property.GenericArguments.SelectMany(TypesInvolvedIn).ToList());
                    break;
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

    private sealed record DataMember(string Name, IType Type, IReadOnlyList<IType> ElementTypes);
}
