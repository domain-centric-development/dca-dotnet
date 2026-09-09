using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace DomainCentric.ArchRules.Rules;

/// <summary>
/// Use case and mapping patterns: the generic input-port contract, Command/Query/Result models, HTTP
/// response models, domain-event publication after saving, DTO-free inner layers, and one consistent
/// use-case namespace depth per module (flat, or grouped by feature), and results that carry values
/// rather than aggregate roots or entities.
/// </summary>
public sealed class UseCaseRules : IDcaRuleSet
{
    /// <summary>Java rules of this set that have no .NET counterpart (id → reason).</summary>
    public static readonly IReadOnlyDictionary<string, string> NotApplicable = new Dictionary<string, string>
    {
        ["DCA-USE-013"] = "Remote-call containment in explicit delegate boundaries cannot be established by this declaration-anchored rule. USE-012 checks publication entry-path coverage, not runtime block containment; review remote effects manually",
    };

    public UseCaseRules(DcaLayout layout)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Rules = new IDcaRule[]
        {
            BaseInputPortResidesInBuildingBlocks(layout),
            CommandsResideInApplication(layout),
            QueriesResideInApplication(layout),
            CommandsAreImmutable(layout),
            QueriesAreImmutable(layout),
            ResultsResideInApplication(layout),
            ResultsAreImmutable(layout),
            ResponsesResideInIncomingAdapters(layout),
            UseCasesPublishDomainEventsAfterSaving(layout),
            NoDtosInDomain(layout),
            NoDtosInApplication(layout),
            PublishingUseCasesAreTransactional(layout),
            UseCasePackagesUseOneDepth(layout),
            ResultsMustNotExposeAggregatesOrEntities(layout),
            UseCasesMustNotInvokeOtherUseCases(layout),
            UseCasesExposeOnlyInputPort(layout),
        };
    }

    public static IDcaRule UseCasesMustNotInvokeOtherUseCases(DcaLayout layout) =>
        DcaRule.Check("DCA-USE-016", "Use cases must not invoke other use cases",
            "Ordinary operations do not coordinate other operations; coordination requires explicit transaction and failure semantics", OperationPolicy.Invocation)
        .Selecting("Concrete non-nested application operations selected by IInputPort marker or configured use-case suffix, with loadable runtime types.")
        .Checking("Reports dependencies on another input port or operation, directly or through same-module application helpers. Own interfaces/base classes are excluded. Messages start Caller -> Target [via Helper], permitting an anchored caller-side coordinator ignore without permitting calls to the coordinator. Reflection, container lookups and unrelated interfaces are not resolved. CYC-005 still checks cycles.");

    public static IDcaRule UseCasesExposeOnlyInputPort(DcaLayout layout) =>
        DcaRule.Check("DCA-USE-017", "Use cases expose no public operation outside their input port",
            "The input port describes the complete externally callable operation surface", OperationPolicy.Surface)
        .Selecting("Concrete non-nested application operations selected by IInputPort marker or configured use-case suffix, with loadable runtime types.")
        .Checking("The effective public instance surface maps through the implemented IInputPort interface maps. Inherited and explicit implementations pass; unrelated-interface methods and public properties outside the contract fail. Constructors, object/ValueType methods and compiler-generated members are exempt; special-name property accessors alone are not exempt.");

    public string Name => "usecase";

    public IReadOnlyList<IDcaRule> Rules { get; }

    /// <summary>The layout this rule set was built for.</summary>
    public DcaLayout Layout { get; }

    public static IDcaRule BaseInputPortResidesInBuildingBlocks(DcaLayout layout) =>
        DcaRule.Of(
            "DCA-USE-001",
            "The base InputPort contract is not redeclared in the project",
            "Base IInputPort interface defines the generic contract for all use cases (Hexagonal Architecture)",
            arch =>
                Interfaces()
                    .That()
                    .HaveNameMatching("^I?InputPort$")
                    .Should()
                    .ResideInNamespaceMatching(DcaLayout.Exactly(DcaLayout.BuildingBlocksPortsInNamespace)))
        .Selecting(
            "Interfaces named InputPort or IInputPort anywhere in the loaded assemblies.")
        .Checking(
            "The interface resides in the building-blocks namespace Hexagonal.Ports.In - the"
                + " generic contract is not redeclared in the project.");

    public static IDcaRule CommandsResideInApplication(DcaLayout layout) =>
        DcaRule.Of(
            "DCA-USE-002",
            "Use Case Commands must end with 'Command' and reside in application namespace",
            "Use case commands should be in application layer (CQRS pattern)",
            arch =>
                Types()
                    .That()
                    .HaveNameEndingWith("Command")
                    .And()
                    .ResideInNamespaceMatching(DcaLayout.Below(layout.RootNamespace))
                    .Should()
                    .ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllApplicationPatterns())))
        .Selecting(
            "Types under the root namespace whose name ends with Command.")
        .Checking(
            "Each resides in an application namespace of some module root"
                + " (<module>.Application). A Command in a domain, adapter or infrastructure"
                + " namespace is reported.");

    public static IDcaRule QueriesResideInApplication(DcaLayout layout) =>
        DcaRule.Of(
            "DCA-USE-003",
            "Use Case Queries must end with 'Query' and reside in application namespace",
            "Use case queries should be in application layer (CQRS pattern)",
            arch =>
                Types()
                    .That()
                    .HaveNameEndingWith("Query")
                    .And()
                    .ResideInNamespaceMatching(DcaLayout.Below(layout.RootNamespace))
                    .Should()
                    .ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllApplicationPatterns())))
        .Selecting(
            "Types under the root namespace whose name ends with Query.")
        .Checking(
            "Each resides in an application namespace of some module root"
                + " (<module>.Application).");

    public static IDcaRule CommandsAreImmutable(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-USE-004",
            "Use Case Commands should be immutable (sealed or records)",
            "Use case commands should be immutable (value objects)",
            arch => ImmutableApplicationModels(arch, "Command"))
        .Selecting("Classes and structs in each module application namespace with the corresponding Command, Query or Result suffix, including record classes and record structs.")
        .Checking(
            "Classes are sealed or records; structs are allowed. Every inherited instance field is readonly, every property is get-only or init-only and no instance Set*(x): void method exists. Referenced objects and collection contents are not inspected." );

    public static IDcaRule QueriesAreImmutable(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-USE-005",
            "Use Case Queries should be immutable (sealed or records)",
            "Use case queries should be immutable (value objects)",
            arch => ImmutableApplicationModels(arch, "Query"))
        .Selecting("Classes and structs in each module application namespace with the corresponding Command, Query or Result suffix, including record classes and record structs.")
        .Checking(
            "Classes are sealed or records; structs are allowed. Every inherited instance field is readonly, every property is get-only or init-only and no instance Set*(x): void method exists. Referenced objects and collection contents are not inspected." );

    public static IDcaRule ResultsResideInApplication(DcaLayout layout) =>
        DcaRule.Of(
            "DCA-USE-006",
            "Use Case Result Models must end with 'Result' and reside in application namespace",
            "Use case result models should be in application layer. Domain Value Objects with 'Result' in name are allowed in domain layer.",
            arch =>
                Types()
                    .That()
                    .HaveNameEndingWith("Result")
                    .And()
                    .ResideInNamespaceMatching(DcaLayout.Below(layout.RootNamespace))
                    .And()
                    .DoNotImplementInterface(typeof(IValue))
                    .Should()
                    .ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllApplicationPatterns())))
        .Selecting(
            "Types under the root namespace whose name ends with Result and that do not"
                + " implement IValue.")
        .Checking(
            "Each resides in an application namespace of some module root"
                + " (<module>.Application). A domain value object named *Result is exempt because it"
                + " implements IValue.");

    public static IDcaRule ResultsAreImmutable(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-USE-007",
            "Use Case Result Models should be immutable (sealed or records)",
            "Use case result models should be immutable (value objects)",
            arch => ImmutableApplicationModels(arch, "Result"))
        .Selecting("Classes and structs in each module application namespace with the corresponding Command, Query or Result suffix, including record classes and record structs.")
        .Checking(
            "Classes are sealed or records; structs are allowed. Every inherited instance field is readonly, every property is get-only or init-only and no instance Set*(x): void method exists. Referenced objects and collection contents are not inspected." );

    public static IDcaRule ResponsesResideInIncomingAdapters(DcaLayout layout) =>
        // Matched by pattern: every incoming adapter, in any context or none, including the shared
        // kernel's adapter where cross-cutting Response classes typically live.
        DcaRule.Of(
            "DCA-USE-008",
            "HTTP Response Models must end with 'Response' and reside in adapter namespace",
            "HTTP response models should be in adapter layer",
            arch =>
                Types()
                    .That()
                    .HaveNameEndingWith("Response")
                    .And()
                    .ResideInNamespaceMatching(DcaLayout.Below(layout.RootNamespace))
                    .Should()
                    .ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllAdapterPatterns())))
        .Selecting(
            "Types under the root namespace whose name ends with Response.")
        .Checking(
            "Each resides in an incoming-adapter namespace of some module root"
                + " (<module>.Adapter or below), the shared kernel's included.");

    /// <summary>
    /// Checked per entry path over the class-internal call graph (<see cref="IntraClassCalls"/>): for every unit that
    /// calls <c>IRepository.SaveAsync</c>, every entry point reaching it — a method callable from outside the class, or
    /// one no method of the class calls — must also reach a <c>PublishAndClearEventsAsync</c>. An entry method may save through one helper and publish
    /// through another; a helper two entry methods share does not connect them.
    /// </summary>
    public static IDcaRule UseCasesPublishDomainEventsAfterSaving(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-USE-009",
            "Use cases that save an aggregate must publish its domain events",
            "A saved aggregate must not keep its events: unpublished, they are lost, and stored on the"
                + " instance they may later be published out of context. Publishing belongs after the"
                + " save, in the use case that owns the unit of work - unless the aggregate is proven never to register an"
                + " event. Checked per entry path, following calls within the use case class: every"
                + " entry point that reaches a save - a method callable from outside the class, or one"
                + " nothing in the class calls - must also reach a publication; a wrapper that publishes"
                + " does not cover a direct call of the public method it wraps, and a helper two methods"
                + " share does not connect them. That the"
                + " publication follows the save and concerns the same aggregate is not established"
                + " statically. Only IDomainEventPublisher.PublishAndClearEventsAsync counts as a"
                + " publication: iterating DomainEvents and calling PublishAsync(event), even followed by"
                + " ClearDomainEvents(), separates dispatch from acknowledgement and is not accepted",
            arch =>
            {
                var violations = new List<string>();
                var useCases = arch.Classes
                    .Where(c => c.Namespace is not null
                        && Matches(c.Namespace.FullName, DcaLayout.AnyOf(arch.AllApplicationPatterns()))
                        && (IsAssignableTo(arch, c, typeof(IInputPort)) || c.Name.Split('`')[0].EndsWith(arch.Layout.UseCaseSuffix, StringComparison.Ordinal)))
                    .OrderBy(c => c.FullName, StringComparer.Ordinal);
                foreach (var useCase in useCases)
                {
                    var runtime = arch.RuntimeType(useCase);
                    if (runtime is null)
                    {
                        continue;
                    }

                    var calls = new IntraClassCalls(runtime);
                    foreach (var unit in calls.Units.Where(u => IntraClassCalls.Calls(u, "SaveAsync", typeof(IRepository))))
                    {
                        if (IntraClassCalls.IlCalls(unit).Where(m => m.Name == "SaveAsync" && m.DeclaringType is not null && typeof(IRepository).IsAssignableFrom(m.DeclaringType))
                            .All(m => EventFreeAggregate.Repository(m, arch))) continue;
                        foreach (var entry in calls.EntryPointsOf(unit))
                        {
                            var publishes = calls.ReachableFrom(entry)
                                .Any(u => IntraClassCalls.Calls(u, "PublishAndClearEventsAsync", typeof(IDomainEventPublisher)));
                            if (!publishes)
                            {
                                violations.Add($"{useCase.FullName}.{IntraClassCalls.PathName(entry, unit)} saves an aggregate without"
                                    + " publishing its domain events - no method reached from there calls PublishAndClearEventsAsync");
                            }
                        }
                    }
                }

                DcaRule.Fail(
                    "Use cases that save an aggregate must publish its domain events",
                    violations.Distinct().ToList(),
                    "call IDomainEventPublisher.PublishAndClearEventsAsync(aggregate) after IRepository.SaveAsync(aggregate) on every path that saves");
            })
        .Selecting(
            "Classes in <module>.Application of every module root selected by IInputPort assignability or the"
                + " configured use-case suffix, whose runtime type is in the loaded assemblies.")
        .Checking(
            "Only a resolved IRepository<T,ID> aggregate with its complete non-building-block hierarchy under scan and no registration call, including helpers, is exempt. Unresolved arguments, partial scans and undecidable external helpers are not exempt. For every non-exempt method calling IRepository.SaveAsync, every entry point"
                + " reaching it (a method callable from outside the class, or one nothing in the"
                + " class calls) also reaches, through calls within the class, a call of"
                + " IDomainEventPublisher.PublishAndClearEventsAsync. Calls are read from the IL of"
                + " the class and its nested state-machine and closure types, so async methods and"
                + " lambdas are followed. Only PublishAndClearEventsAsync counts - PublishAsync(event),"
                + " even followed by ClearDomainEvents(), does not. A use case without a save (a query,"
                + " a bulk delete) is selected but has nothing to check and passes.");

    public static IDcaRule PublishingUseCasesAreTransactional(DcaLayout layout) =>
        DcaRule.Check("DCA-USE-012", "Publishing use cases require a transaction boundary on every entry path",
            "Integration-event capture joins the modeled transaction; publication outside a transaction cannot rely on commit semantics",
            arch => {
                var violations = new List<string>();
                foreach (var type in arch.Types.Where(t => OperationPolicy.Operation(t, arch))) {
                    var runtime = arch.RuntimeType(type)!;
                    var graph = new IntraClassCalls(runtime);
                    foreach (var publisher in graph.Units.Where(u => IntraClassCalls.IlCalls(u).Any(m => m.DeclaringType is not null && typeof(IDomainEventPublisher).IsAssignableFrom(m.DeclaringType)))) {
                        foreach (var entry in graph.EntryPointsOf(publisher)) {
                            if (!Transactional(runtime, layout.FrameworkTypes.TransactionalAttribute)
                                && graph.ReachableThrough(entry, u => !Transactional(u, layout.FrameworkTypes.TransactionalAttribute) && !CallsBoundary(u)).Contains(publisher))
                                violations.Add($"{type.FullName}.{IntraClassCalls.PathName(entry, publisher)} publishes without transaction coverage on every entry path");
                        }
                    }
                }
                DcaRule.Fail("Publishing use cases require transaction coverage", violations.Distinct().ToList());
            })
        .Selecting("Concrete application operations selected by IInputPort marker or suffix with loadable runtime types, including closure and async units.")
        .Checking("Every entry path to an IDomainEventPublisher call crosses a unit calling ITransactionBoundary.InTransactionAsync (including its extension overloads) or carrying the configured TransactionalAttribute; a type-level attribute covers all paths. Static limit: a boundary call in the same unit passes even when publication follows an empty boundary block. Runtime rollback containment must be tested separately.");

    private static bool CallsBoundary(MethodBase unit) => IntraClassCalls.IlCalls(unit).Any(m => m.Name == "InTransactionAsync"
        && ((m.DeclaringType is not null && typeof(DomainCentric.BuildingBlocks.Application.Transactions.ITransactionBoundary).IsAssignableFrom(m.DeclaringType))
            || (m.IsStatic && m.GetParameters().FirstOrDefault()?.ParameterType == typeof(DomainCentric.BuildingBlocks.Application.Transactions.ITransactionBoundary))));

    private static bool Transactional(MemberInfo target, string attributeName)
    {
        if (string.IsNullOrWhiteSpace(attributeName)) return false;
        return target.GetCustomAttributesData().Any(a => {
            for (var type = a.AttributeType; type is not null; type = type.BaseType) if (type.FullName == attributeName) return true;
            return false;
        });
    }

    public static IDcaRule NoDtosInDomain(DcaLayout layout) =>
        DcaRule.Of(
            "DCA-USE-010",
            "DTOs must not be used in the Domain Layer",
            "Domain layer should not depend on DTOs (presentation concerns) - Dependency Inversion Principle",
            arch =>
                Types()
                    .That()
                    .ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllDomainPatterns()))
                    .Should()
                    .NotDependOnAnyTypesThat()
                    .HaveNameEndingWith("Dto"))
        .Selecting(
            "Types in <module>.Domain of every module root.")
        .Checking(
            "No dependency on a type whose name ends with Dto.");

    public static IDcaRule NoDtosInApplication(DcaLayout layout) =>
        DcaRule.Of(
            "DCA-USE-011",
            "DTOs must not be used in the Application Layer",
            "Application layer should use Command/Query/Response models, not presentation DTOs (Clean Architecture)",
            arch =>
                Types()
                    .That()
                    .ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllApplicationPatterns()))
                    .Should()
                    .NotDependOnAnyTypesThat()
                    .HaveNameEndingWith("Dto"))
        .Selecting(
            "Types in <module>.Application of every module root.")
        .Checking(
            "No dependency on a type whose name ends with Dto. Command, Query and Result"
                + " models are not DTOs by this rule's definition - only the Dto suffix is checked.");

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Application models with the given suffix must be immutable: a record, a struct (value type) or a
    /// sealed class (the .NET reading of Java's <c>final</c>). Interfaces are not checked.
    /// </summary>
    /// <summary>
    /// Use cases live at one of two depths below a module's application namespace:
    /// <c>Application.&lt;UseCase&gt;</c> (flat) or <c>Application.&lt;Feature&gt;.&lt;UseCase&gt;</c> (grouped).
    /// Selects the classes ending in the configured use-case suffix, ignores <c>Application.Shared</c>, abstract
    /// classes (a shared base class is not a use case) and nested types, and reports every offending module and namespace in one violation: a use case directly in the
    /// application namespace, one nested deeper than a feature, or a module that mixes both forms.
    /// </summary>
    public static IDcaRule UseCasePackagesUseOneDepth(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-USE-014",
            "Use case namespaces within a module must use one consistent depth (flat or grouped by feature)",
            "A use case namespace sits either directly below the application namespace (Application.<UseCase>) or"
                + " one level deeper inside a feature (Application.<Feature>.<UseCase>). A feature is an optional,"
                + " domain-named group of related use cases - a navigation boundary inside one bounded context, not"
                + " a layer, module or aggregate owner. Mixing both forms in one module makes it unclear whether a"
                + " namespace is a feature, a use case or a leftover; nesting deeper than a feature hides the use"
                + " case. The rule checks legibility only: it does not infer bounded contexts, feature semantics or"
                + " aggregate ownership. Application.Shared holds the context-wide output ports and is not a use"
                + " case namespace",
            arch => CheckUseCaseDepth(arch, layout))
        .Selecting(
            "Per module root: non-abstract, non-nested classes below <module>.Application"
                + " that implement IInputPort or whose name ends with the configured use-case suffix, excluding Application.Shared"
                + " and everything below it.")
        .Checking(
            "After removing configured OperationContainers segments, all of them sit at one depth: Application.<UseCase> (flat) or"
                + " Application.<Feature>.<UseCase> (grouped). Reported are a use case directly in the"
                + " application namespace, one nested deeper than a feature, and a module mixing both"
                + " depths. What a feature means is not checked.");

    private static void CheckUseCaseDepth(DcaArchitecture arch, DcaLayout layout)
    {
        var violations = new List<string>();
        foreach (var root in arch.ModuleRoots())
        {
            var application = root + "." + layout.ApplicationSegment;
            var shared = application + ".Shared";
            var byDepth = new SortedDictionary<int, SortedSet<string>>();
            foreach (var candidate in arch.Classes)
            {
                var ns = candidate.Namespace?.FullName;
                if (ns is null
                    || !(ns == application || ns.StartsWith(application + ".", StringComparison.Ordinal))
                    || ns == shared
                    || ns.StartsWith(shared + ".", StringComparison.Ordinal)
                    || candidate.IsNested
                    || candidate.IsAbstract == true
                    || !(IsAssignableTo(arch, candidate, typeof(IInputPort)) || candidate.Name.EndsWith(layout.UseCaseSuffix, StringComparison.Ordinal)))
                {
                    continue;
                }

                var depth = ns == application ? 0 : ns.Substring(application.Length + 1).Split('.').Count(segment => !layout.OperationContainers.Contains(segment));
                if (!byDepth.TryGetValue(depth, out var namespaces))
                {
                    byDepth[depth] = namespaces = new SortedSet<string>(StringComparer.Ordinal);
                }

                namespaces.Add(ns);
            }

            if (byDepth.Count == 0)
            {
                continue;
            }

            if (byDepth.TryGetValue(0, out var shallow))
            {
                violations.AddRange(shallow.Select(ns =>
                    $"Module {root}: use case directly in the application namespace {ns} - give it a namespace of its own (Application.<UseCase>)"));
            }

            foreach (var (depth, namespaces) in byDepth.Select(e => (e.Key, e.Value)))
            {
                if (depth > 2)
                {
                    violations.AddRange(namespaces.Select(ns =>
                        $"Module {root}: use case namespace {ns} is nested deeper than Application.<Feature>.<UseCase>"));
                }
            }

            if (byDepth.ContainsKey(1) && byDepth.ContainsKey(2))
            {
                violations.Add(
                    $"Module {root} mixes flat use case namespaces [{string.Join(", ", byDepth[1])}] with feature-grouped ones"
                    + $" [{string.Join(", ", byDepth[2])}] - finish the migration in one direction");
            }
        }

        DcaRule.Fail(
            "Use case namespaces within a module must use one consistent depth (flat or grouped by feature)",
            violations,
            "keep every use case of the module at Application.<UseCase>, or group all of them as Application.<Feature>.<UseCase>");
    }

    /// <summary>
    /// A use case result carries the answer, not the model: no property, field or generic type argument of a
    /// <c>*Result</c> type in an application namespace may be assignable to <see cref="IAggregateRoot"/> or
    /// <see cref="IEntity"/>. The walk is transitive: it follows nested records, part records anywhere in the
    /// application layer (<c>Application.Shared</c> included; parts carry no <c>Result</c> suffix), arrays, generic
    /// arguments (<c>IReadOnlyList&lt;T&gt;</c>, <c>IReadOnlyDictionary&lt;K,V&gt;</c>) and <c>Nullable&lt;T&gt;</c>,
    /// and reports the member path of every
    /// identity it finds. Domain value objects named <c>*Result</c> (<see cref="IValue"/>) are not results.
    /// </summary>
    public static IDcaRule ResultsMustNotExposeAggregatesOrEntities(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-USE-015",
            "Use Case Result Models must not expose aggregate roots or entities",
            "A result is the use case's answer, not a handle on the model: identity and behaviour stay behind"
                + " the port; values, enriched models and read models may cross. Checked transitively through"
                + " nested records, part records anywhere in the application layer (Application.Shared included),"
                + " arrays and generic type arguments (IReadOnlyList<T>, T?, IReadOnlyDictionary<K,V>)",
            CheckResultsCarryNoIdentities)
        .Selecting("Top-level classes and structs in each module application namespace ending in Result, excluding IValue types.")
        .Checking(
            "No public instance result member transitively through application part records and generic wrappers exposes IAggregateRoot or IEntity; classes and structs are checked. Domain values and read models are not traversed." );

    private static void CheckResultsCarryNoIdentities(DcaArchitecture arch)
    {
        var application = DcaLayout.AnyOf(arch.AllApplicationPatterns());
        var violations = new List<string>();
        foreach (var result in arch.Types.Where(t => t is Class or Struct)
            .Where(c => c.Namespace is not null
                && Matches(c.Namespace.FullName, application)
                && !c.IsNested
                && c.Name.EndsWith("Result", StringComparison.Ordinal)
                && !IsAssignableTo(arch, c, typeof(IValue)))
            .OrderBy(c => c.FullName, StringComparer.Ordinal))
        {
            var runtime = arch.RuntimeType(result);
            if (runtime is null)
            {
                continue;
            }

            WalkResultMembers(application, runtime, runtime.Name, new HashSet<Type>(), violations);
        }

        DcaRule.Fail(
            "Use Case Result Models must not expose aggregate roots or entities",
            violations.Distinct().OrderBy(v => v, StringComparer.Ordinal).ToList(),
            "carry ids, values, read models or snapshots instead");
    }

    /// <summary>
    /// Walks the instance members (any visibility) of a result or part record — inherited ones included, a base class
    /// need not carry the suffix. <paramref name="onPath"/> holds the records currently being walked and
    /// guards against a self-referencing part record; it is not a global visited set, so the same part
    /// record reached through two members is reported on both paths.
    /// </summary>
    private static void WalkResultMembers(string application, Type type, string path, ISet<Type> onPath, ICollection<string> violations)
    {
        if (!onPath.Add(type))
        {
            return;
        }

        const BindingFlags members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var memberTypes = type.GetProperties(members).Select(p => (p.Name, p.PropertyType))
            .Concat(type.GetFields(members).Where(f => !f.Name.EndsWith("k__BackingField", StringComparison.Ordinal)).Select(f => (f.Name, f.FieldType)))
            .OrderBy(m => m.Name, StringComparer.Ordinal);
        foreach (var (name, memberType) in memberTypes)
        {
            if (name == "EqualityContract")
            {
                continue;
            }

            CheckResultMemberType(application, memberType, $"{path}.{name}", onPath, violations);
        }

        onPath.Remove(type);
    }

    /// <summary>
    /// One member type: an identity is a violation; an array is checked through its element type; a generic
    /// instantiation through every type argument <em>and</em> its own members (a generic part record may hide an
    /// identity next to its type parameter); a part record through its members.
    /// </summary>
    private static void CheckResultMemberType(string application, Type type, string path, ISet<Type> onPath, ICollection<string> violations)
    {
        var identity = IdentityMarkerOf(type);
        if (identity is not null)
        {
            violations.Add($"{path} : {type.Name} ({identity})");
            return;
        }

        if (type.IsArray)
        {
            CheckResultMemberType(application, type.GetElementType()!, path, onPath, violations);
            return;
        }

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                CheckResultMemberType(application, argument, path, onPath, violations);
            }
        }

        if (IsPartRecord(application, type))
        {
            WalkResultMembers(application, type, $"{path} -> {PlainName(type)}", onPath, violations);
        }
    }

    /// <summary>
    /// A part record: a record (class or struct) declared in an application namespace — nested in the result, next
    /// to it, or shared in <c>Application.Shared</c>. Types from other layers (value objects, read models) are values
    /// by contract and are not walked; BCL and generic parameters never are.
    /// </summary>
    private static bool IsPartRecord(string application, Type type) =>
        !type.IsGenericParameter
        && !type.IsPrimitive
        && !type.IsEnum
        && type.Namespace is not null
        && Matches(type.Namespace, application)
        && IsRecord(type);

    /// <summary>Record classes synthesize <c>&lt;Clone&gt;$</c>; record structs synthesize <c>PrintMembers</c> and <c>Deconstruct</c> like record classes do.</summary>
    private static bool IsRecord(Type type) =>
        type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null
        || (type.IsValueType && type.GetMethod("PrintMembers", BindingFlags.NonPublic | BindingFlags.Instance) is not null);

    private static string PlainName(Type type)
    {
        var tick = type.Name.IndexOf('`');
        return tick >= 0 ? type.Name.Substring(0, tick) : type.Name;
    }

    private static string? IdentityMarkerOf(Type type) =>
        typeof(IAggregateRoot).IsAssignableFrom(type) ? nameof(IAggregateRoot)
        : typeof(IEntity).IsAssignableFrom(type) ? nameof(IEntity)
        : null;

    private static void ImmutableApplicationModels(DcaArchitecture arch, string suffix)
    {
        var violations = arch.Types.Where(t => t is Class or Struct)
            .Where(c => c.Namespace is not null
                && Matches(c.Namespace.FullName, DcaLayout.AnyOf(arch.AllApplicationPatterns()))
                && c.Name.EndsWith(suffix, StringComparison.Ordinal)
                && !TacticalPatternRules.IsImmutableShape(c))
            .Select(c => $"{c.FullName} does not have immutable instance state")
            .ToList();
        DcaRule.Fail(
            $"Use case {suffix} models should be immutable (sealed or records)",
            violations,
            $"declare *{suffix} types with immutable instance state");
    }

    private static bool Matches(string ns, string pattern) =>
        System.Text.RegularExpressions.Regex.IsMatch(ns, pattern);

    /// <summary>
    /// Assignability via the ArchUnitNET model (implemented interfaces, base classes) with a reflection
    /// fallback for types whose interface closure the loader did not resolve.
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
