using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
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
    public static readonly IReadOnlyDictionary<string, string> NotApplicable = new Dictionary<string, string>();

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
            MutatingUseCasesAreTransactional(layout),
            TransactionalUseCasesDoNotCallRemotePorts(layout),
            UseCasePackagesUseOneDepth(layout),
            ResultsMustNotExposeAggregatesOrEntities(layout),
            UseCasesMustNotInvokeOtherUseCases(layout),
            UseCasesExposeOnlyInputPort(layout),
            IncomingAdaptersDriveOneFeature(layout),
            QueryUseCasesDoNotWrite(layout),
            UseCasesDoNotParseIds(layout),
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
        .Checking("The effective public instance surface maps through the implemented IInputPort interface maps. Inherited and explicit implementations pass; unrelated-interface methods and public properties outside the contract fail (a property is reported once). Constructors, object/ValueType methods and compiler-generated members are exempt; special-name property accessors alone are not exempt. A use case selected by suffix only, without an input-port interface, has no permitted operation and is reported in full — implement the input port.");

    /// <summary>
    /// DCA-USE-018 (experimental). Once the application is grouped into features, the incoming adapter is cut the
    /// same way: an adapter class that drives the commands of two features grows with every use case of both.
    /// Reading a neighbouring feature's query is no reason to cut, so only command ports count. The feature of an
    /// input port is read the way <c>DCA-USE-014</c> reads it — the segment below the application
    /// namespace when the port sits at <c>Application.&lt;Feature&gt;.&lt;UseCase&gt;</c>.
    /// </summary>
    public static IDcaRule IncomingAdaptersDriveOneFeature(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-USE-018",
            "In a module grouped by feature, incoming adapters must drive the commands of one feature only",
            "Once the application is grouped into features, the incoming adapter is cut the same way: one"
                + " adapter changes through the use cases of one feature. An adapter that drives the commands of"
                + " two features grows with every use case of both and is cut along them. Reading a neighbouring"
                + " feature's query is no reason to cut",
            arch =>
            {
                var violations = new List<string>();
                foreach (var adapter in arch.Classes
                    .Where(c => !c.IsCompilerGenerated && c.Namespace is not null && arch.InIncomingAdapter(c))
                    .OrderBy(c => c.FullName, StringComparer.Ordinal))
                {
                    var root = arch.ModuleRootOf(adapter.Namespace!.FullName);
                    if (root is null)
                    {
                        continue;
                    }

                    var features = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
                    foreach (var port in InputPortsOf(arch, adapter).Where(p => PortInputs(p, arch.Layout.Markers).Any(t => arch.Layout.Markers.Is("Command", t))))
                    {
                        var feature = FeatureOf(port.Namespace ?? "", root, layout);
                        if (feature is null)
                        {
                            continue;
                        }

                        if (!features.TryGetValue(feature, out var ports))
                        {
                            features[feature] = ports = new SortedSet<string>(StringComparer.Ordinal);
                        }

                        ports.Add(port.Name.Split('`')[0]);
                    }

                    if (features.Count > 1)
                    {
                        violations.Add($"{adapter.FullName} drives the commands of {features.Count} features: "
                            + string.Join("; ", features.Select(f => $"{f.Key} ({string.Join(", ", f.Value)})")));
                    }
                }

                DcaRule.Fail(
                    "In a module grouped by feature, incoming adapters must drive the commands of one feature only",
                    violations,
                    "cut the adapter along the features - one adapter class per feature, below the same protocol segment");
            })
        .Experimental()
        .Selecting(
            "Classes in <module>.Adapter.Incoming of every module root, compiler-generated classes excluded,"
                + " with the command ports they depend on: types assignable to IInputPort in the same module's"
                + " application namespace whose input is a command (the configured command role, by default a simple"
                + " name ending with Command) - the input type argument of IUseCase<TInput, TOutput> or a parameter of"
                + " an input-port method. A port counts toward a feature when it sits at"
                + " Application.<Feature>.<UseCase> after removing configured OperationContainers segments, the"
                + " grouped depth of DCA-USE-014.")
        .Checking(
            "The command ports an adapter depends on belong to one feature. A port whose input is a query, or"
                + " neither, is not counted - reading a neighbouring feature's query is allowed. A port in a"
                + " flat use-case namespace"
                + " (Application.<UseCase>), in Application.Shared or in another module counts toward no"
                + " feature, so a module that is not grouped into features is never reported. Fields,"
                + " constructor parameters and calls all count as a dependency; constructor parameters are read"
                + " from the runtime type as well. An empty selection passes.");

    /// <summary>
    /// DCA-USE-019 (experimental). A read never creates: a use case whose input is a query saves, deletes and
    /// publishes nothing.
    /// </summary>
    public static IDcaRule QueryUseCasesDoNotWrite(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-USE-019",
            "Query use cases must not write or publish",
            "A read never creates. A query has no side effect: it does not create, save or publish, and when"
                + " nothing exists it answers none. Find-or-create is a command, called where the caller is about"
                + " to change state; a caller that only shows what exists asks the query",
            arch =>
            {
                var violations = new List<string>();
                var markers = arch.Layout.Markers;
                foreach (var type in arch.Types.Where(t => OperationPolicy.Operation(t, arch)).OrderBy(t => t.FullName, StringComparer.Ordinal))
                {
                    var runtime = arch.RuntimeType(type)!;
                    if (!InputTypesOf(runtime, markers).Any(t => markers.Is("Query", t)))
                    {
                        continue;
                    }

                    foreach (var unit in new IntraClassCalls(runtime).Units)
                    {
                        foreach (var call in IntraClassCalls.IlCalls(unit))
                        {
                            var effect = WriteEffectOf(call, markers);
                            if (effect is not null)
                            {
                                violations.Add($"{type.FullName}.{IntraClassCalls.DisplayName(unit)} takes a query and {effect}"
                                    + $" ({call.DeclaringType!.Name.Split('`')[0]}.{call.Name})");
                            }
                        }
                    }
                }

                DcaRule.Fail(
                    "Query use cases must not write or publish",
                    violations.Distinct().ToList(),
                    "answer none when nothing exists, and move the write into a command use case the caller runs where it is about to change state");
            })
        .Experimental()
        .Selecting(
            "Concrete non-nested application operations selected by IInputPort marker or configured use-case"
                + " suffix, with loadable runtime types, whose input ends in Query: the input type argument of an"
                + " implemented IUseCase<TInput, TOutput>, or a parameter of a method that implements an"
                + " IInputPort interface.")
        .Checking(
            "No method of the class - async state machines and lambdas included - calls a repository write"
                + " (IRepository.SaveAsync or DeleteByIdAsync, or a method of a repository or store port whose"
                + " name starts with Save, Delete, Remove, Add, Insert, Update, Upsert, Put, Store, Write,"
                + " Create, Append, Archive, Purge, Clear, Set or Mark), or any method of the domain-event or"
                + " integration-event publisher. Only the class's own calls are read: a write reached through"
                + " a helper class or a second port is not seen, and a store write under another verb passes."
                + " An empty selection passes.");

    /// <summary>
    /// DCA-USE-020 (experimental). The adapter turns raw input into the types the input port takes; an id arrives
    /// as the id's value type, never as text the application parses.
    /// </summary>
    public static IDcaRule UseCasesDoNotParseIds(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-USE-020",
            "Use cases must not parse raw input into an id",
            "The adapter turns raw input into the types the input port takes: an id arrives in the command as"
                + " the id's value type, parsed at the edge or by the value type's own factory, never as text the"
                + " use case parses. Text that does not parse is a malformed call the adapter answers itself;"
                + " the use case never sees it",
            arch =>
            {
                var violations = new List<string>();
                foreach (var type in arch.Types
                    .Where(t => t.Namespace is not null && arch.InApplication(t))
                    .OrderBy(t => t.FullName, StringComparer.Ordinal))
                {
                    var runtime = arch.RuntimeType(type);
                    if (runtime is null || runtime.IsNested)
                    {
                        continue;
                    }

                    foreach (var unit in new IntraClassCalls(runtime).Units)
                    {
                        foreach (var call in IntraClassCalls.IlCalls(unit).Where(c => c.DeclaringType == typeof(Guid) && IdParseMethods.Contains(c.Name)))
                        {
                            violations.Add($"{type.FullName}.{IntraClassCalls.DisplayName(unit)} parses an id with Guid.{call.Name}");
                        }
                    }
                }

                DcaRule.Fail(
                    "Use cases must not parse raw input into an id",
                    violations.Distinct().ToList(),
                    "let the input port take the id's value type and parse the text in the adapter, or in the value type's own factory");
            })
        .Experimental()
        .Selecting(
            "Non-nested types in <module>.Application of every module root with their runtime type in the"
                + " loaded assemblies - use cases, helpers and application services alike - together with"
                + " their nested, async state-machine and closure types.")
        .Checking(
            "No method calls the platform's parse API for a unique identifier: System.Guid.Parse, TryParse,"
                + " ParseExact or TryParseExact, in any overload. Parsing a project's own id type, or an id"
                + " held as a number or another text format, is not seen; the value type's own factory in the"
                + " domain is outside the selection and may parse. An empty selection passes.");

    private static readonly HashSet<string> IdParseMethods = new(StringComparer.Ordinal) { "Parse", "TryParse", "ParseExact", "TryParseExact" };

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
                    .And()
                    .FollowCustomPredicate(t => t.FullName != arch.Layout.Markers.InputPort, "are not the type the InputPort role names")
                    .Should()
                    .ResideInNamespaceMatching(DcaLayout.AnyOf(
                        new[] { DcaLayout.BuildingBlocksPortsInNamespace }
                            .Concat(arch.Layout.Markers.DeclaringNamespacesOf(new[] { nameof(DcaMarkers.InputPort) }))
                            .Distinct(StringComparer.Ordinal)
                            .Select(DcaLayout.Exactly))))
        .Selecting(
            "Interfaces named InputPort or IInputPort anywhere in the loaded assemblies - except the type the"
                + " InputPort role names.")
        .Checking(
            "The interface resides in the building-blocks namespace Hexagonal.Ports.In or in the namespace of"
                + " the configured InputPort role - the generic contract is declared once, by the vocabulary in"
                + " use, and not redeclared elsewhere in the project.");

    public static IDcaRule CommandsResideInApplication(DcaLayout layout) =>
        DcaRule.Of(
            "DCA-USE-002",
            "Commands reside in the application layer",
            "Use case commands should be in application layer (CQRS pattern)",
            arch =>
                Types()
                    .That()
                    .FollowCustomPredicate(t => arch.Layout.Markers.Is("Command", t), "are commands")
                    .And()
                    .ResideInNamespaceMatching(DcaLayout.Below(layout.RootNamespace))
                    .Should()
                    .FollowCustomCondition(LayerConditions.PlacedIn(arch.InApplication, "the application layer"), "be in the application layer"))
        .Selecting(
            "Types under the root namespace the configured command role recognises - by default a simple name"
                + " ending with Command.")
        .Checking(
            "Each resides in an application namespace of some module root"
                + " (<module>.Application). A Command in a domain, adapter or infrastructure"
                + " namespace is reported.");

    public static IDcaRule QueriesResideInApplication(DcaLayout layout) =>
        DcaRule.Of(
            "DCA-USE-003",
            "Queries reside in the application layer",
            "Use case queries should be in application layer (CQRS pattern)",
            arch =>
                Types()
                    .That()
                    .FollowCustomPredicate(t => arch.Layout.Markers.Is("Query", t), "are queries")
                    .And()
                    .ResideInNamespaceMatching(DcaLayout.Below(layout.RootNamespace))
                    .Should()
                    .FollowCustomCondition(LayerConditions.PlacedIn(arch.InApplication, "the application layer"), "be in the application layer"))
        .Selecting(
            "Types under the root namespace the configured query role recognises - by default a simple name"
                + " ending with Query.")
        .Checking(
            "Each resides in an application namespace of some module root"
                + " (<module>.Application).");

    public static IDcaRule CommandsAreImmutable(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-USE-004",
            "Use Case Commands should be immutable (sealed or records)",
            "Use case commands should be immutable (value objects)",
            arch => ImmutableApplicationModels(arch, "Command", t => arch.Layout.Markers.Is("Command", t)))
        .Selecting("Classes and structs, including record classes and record structs, in each module application namespace that the configured command role recognises - by default a simple name ending with Command.")
        .Checking(
            "Classes are sealed or records; structs are allowed. Every inherited instance field is readonly, every property is get-only or init-only and no instance Set*(x): void method exists. Referenced objects and collection contents are not inspected.");

    public static IDcaRule QueriesAreImmutable(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-USE-005",
            "Use Case Queries should be immutable (sealed or records)",
            "Use case queries should be immutable (value objects)",
            arch => ImmutableApplicationModels(arch, "Query", t => arch.Layout.Markers.Is("Query", t)))
        .Selecting("Classes and structs, including record classes and record structs, in each module application namespace that the configured query role recognises - by default a simple name ending with Query.")
        .Checking(
            "Classes are sealed or records; structs are allowed. Every inherited instance field is readonly, every property is get-only or init-only and no instance Set*(x): void method exists. Referenced objects and collection contents are not inspected.");

    public static IDcaRule ResultsResideInApplication(DcaLayout layout) =>
        DcaRule.Of(
            "DCA-USE-006",
            "Types named *Result reside in the application layer",
            "Use case result models should be in application layer. Domain Value Objects with 'Result' in name are allowed in domain layer.",
            arch =>
                Types()
                    .That()
                    .HaveNameEndingWith("Result")
                    .And()
                    .ResideInNamespaceMatching(DcaLayout.Below(layout.RootNamespace))
                    .And()
                    .FollowCustomPredicate(t => !arch.Layout.Markers.Is(nameof(DcaMarkers.Value), t), "are not value objects")
                    .And()
                    .FollowCustomPredicate(t => !IsClosedDomainOutcome(arch, t), "are not a closed domain outcome")
                    .Should()
                    .FollowCustomCondition(LayerConditions.PlacedIn(arch.InApplication, "the application layer"), "be in the application layer"))
        .Selecting(
            "Types under the root namespace whose name ends with Result, that do not implement IValue and"
                + " that are not a closed domain outcome - in a domain namespace, an abstract class or record"
                + " whose constructors are all private or private protected, so that no other assembly can derive"
                + " from it, or a direct case of one.")
        .Checking(
            "Each resides in an application namespace of some module root"
                + " (<module>.Application). A domain value object named *Result is exempt because it"
                + " implements IValue, and a closed outcome type of the domain - a sum type whose cases the"
                + " domain returns, the C# reading of a Java sealed type - is exempt because it is part of the"
                + " model.");

    public static IDcaRule ResultsAreImmutable(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-USE-007",
            "Use Case Result Models should be immutable (sealed or records)",
            "Use case result models should be immutable (value objects)",
            arch => ImmutableApplicationModels(arch, "Result", t => t.Name.EndsWith("Result", StringComparison.Ordinal)))
        .Selecting("Classes and structs in each module application namespace with the corresponding Command, Query or Result suffix, including record classes and record structs.")
        .Checking(
            "Classes are sealed or records; structs are allowed. Every inherited instance field is readonly, every property is get-only or init-only and no instance Set*(x): void method exists. Referenced objects and collection contents are not inspected.");

    public static IDcaRule ResponsesResideInIncomingAdapters(DcaLayout layout) =>
        // Matched by pattern: every incoming adapter, in any context or none, including the shared
        // kernel's adapter where cross-cutting Response classes typically live.
        DcaRule.Of(
            "DCA-USE-008",
            "Types named *Response reside in an adapter",
            "HTTP response models should be in adapter layer",
            arch =>
                Types()
                    .That()
                    .HaveNameEndingWith("Response")
                    .And()
                    .ResideInNamespaceMatching(DcaLayout.Below(layout.RootNamespace))
                    .Should()
                    .FollowCustomCondition(LayerConditions.PlacedIn(arch.InAdapter, "an adapter"), "be in an adapter"))
        .Selecting(
            "Types under the root namespace whose name ends with Response.")
        .Checking(
            "Each resides in an adapter namespace (incoming or outgoing) of some module root"
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
                        && arch.InApplication(c)
                        && (IsRole(arch, c, nameof(DcaMarkers.InputPort)) || c.Name.Split('`')[0].EndsWith(arch.Layout.UseCaseSuffix, StringComparison.Ordinal)))
                    .OrderBy(c => c.FullName, StringComparer.Ordinal);
                foreach (var useCase in useCases)
                {
                    var runtime = arch.RuntimeType(useCase);
                    if (runtime is null)
                    {
                        continue;
                    }

                    var calls = new IntraClassCalls(runtime);
                    foreach (var unit in calls.Units.Where(u => IntraClassCalls.Calls(u, "SaveAsync", t => arch.Layout.Markers.Is(nameof(DcaMarkers.Repository), t))))
                    {
                        if (IntraClassCalls.IlCalls(unit).Where(m => m.Name == "SaveAsync" && m.DeclaringType is not null && arch.Layout.Markers.Is(nameof(DcaMarkers.Repository), m.DeclaringType))
                            .All(m => EventFreeAggregate.Repository(m, arch)))
                        {
                            continue;
                        }

                        foreach (var entry in calls.EntryPointsOf(unit))
                        {
                            var publishes = calls.ReachableFrom(entry)
                                .Any(u => IntraClassCalls.Calls(u, "PublishAndClearEventsAsync", t => arch.Layout.Markers.Is(nameof(DcaMarkers.DomainEventPublisher), t)));
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
            "Only a resolved IRepository<T,ID> aggregate is exempt, and only when its complete hierarchy is under scan and nothing registers an event on it: neither the hierarchy itself nor a type outside it. The walk stops at the platform and at the namespaces the configured marker roles live in, so a project's own vocabulary stops it where the library's does. Unresolved arguments and partial scans are not exempt. For every non-exempt method calling IRepository.SaveAsync, every entry point"
                + " reaching it (a method callable from outside the class, or one nothing in the"
                + " class calls) also reaches, through calls within the class, a call of"
                + " IDomainEventPublisher.PublishAndClearEventsAsync. Calls are read from the IL of"
                + " the class and its nested state-machine and closure types, so async methods and"
                + " lambdas are followed. Only PublishAndClearEventsAsync counts - PublishAsync(event),"
                + " even followed by ClearDomainEvents(), does not. A use case without a save (a query,"
                + " a bulk delete) is selected but has nothing to check and passes. The method names are fixed and are not part of the marker roles: a vocabulary whose repository writes under another name is selected and then found to save nothing, so this rule passes over it.");

    public static IDcaRule MutatingUseCasesAreTransactional(DcaLayout layout) =>
        DcaRule.Check("DCA-USE-012", "Use cases that save an aggregate or publish domain events must have a transaction boundary",
            "The use case owns the unit of work. Saving an aggregate is one business fact, yet the repository may write it as"
            + " several statements - an aggregate of entities and value objects often spans several tables - and a repository"
            + " adapter draws no boundary of its own; without one, a failure between the statements leaves half an aggregate"
            + " behind. Publishing adds a second effect that must fall with the save: integration-event capture joins the"
            + " modeled transaction, and publication outside a transaction cannot rely on commit semantics. The boundary is"
            + " either the configured transactional attribute (on the class or the executing method) or an explicit"
            + " ITransactionBoundary.InTransactionAsync(...) around load, mutate, save and publish. Checked per entry path,"
            + " following calls within the class: from every entry point - a method callable from outside the class, or one"
            + " nothing in the class calls - no route down to the saving, deleting or publishing method may be free of an"
            + " attribute or a boundary; a covered caller does not cover another route to the same helper, and a boundary on"
            + " one route does not cover a second route",
            arch =>
            {
                var violations = new List<string>();
                foreach (var type in arch.Types.Where(t => OperationPolicy.Operation(t, arch)))
                {
                    var runtime = arch.RuntimeType(type)!;
                    var graph = new IntraClassCalls(runtime);
                    foreach (var unit in graph.Units)
                    {
                        var effect = TransactionalEffectOf(unit, arch.Layout.Markers);
                        if (effect is null)
                        {
                            continue;
                        }

                        foreach (var entry in graph.EntryPointsOf(unit))
                        {
                            if (!Transactional(runtime, layout.FrameworkTypes.TransactionalAttribute)
                                && graph.ReachableThrough(entry, u => !Transactional(u, layout.FrameworkTypes.TransactionalAttribute) && !CallsBoundary(u, arch.Layout.Markers)).Contains(unit))
                            {
                                violations.Add(
                                    $"{type.FullName}.{IntraClassCalls.PathName(entry, unit)} {effect} without "
                                    + DescribeTransactionalAttribute(layout.FrameworkTypes.TransactionalAttribute)
                                    + " on the class or on a method of that path, and without"
                                    + " ITransactionBoundary.InTransactionAsync(...) on it");
                            }
                        }
                    }
                }
                DcaRule.Fail(
                    "Use cases that save an aggregate or publish domain events must have a transaction boundary",
                    violations.Distinct().ToList(),
                    "wrap load, mutate, save and publish in ITransactionBoundary.InTransactionAsync(...),"
                    + " or carry the configured transactional attribute on the class or on every entry path");
            })
        .Selecting("Concrete application operations selected by IInputPort marker or suffix with loadable runtime types, including closure and async units.")
        .Checking("Every entry path to a unit calling IRepository.SaveAsync, IRepository.DeleteByIdAsync or an IDomainEventPublisher crosses a unit calling ITransactionBoundary.InTransactionAsync (including its extension overloads) or carrying the configured TransactionalAttribute; a type-level attribute covers all paths. The declarative path is empty by default in .NET - AspNetCore() and None() both leave TransactionalAttribute unset, because ASP.NET Core has no ambient transaction attribute - so out of the box only the explicit boundary satisfies the rule, while the Java twin also accepts the framework's transactional annotation. A use case that neither saves, deletes nor publishes (a query, a Store write) is selected but has nothing to check and passes. Static limit: a boundary call in the same unit passes even when the save or publication follows an empty boundary block. Runtime rollback containment must be tested separately. The method names are fixed and are not part of the marker roles: a vocabulary whose repository writes under another name is selected and then found to save nothing, so this rule passes over it.");

    /// <summary>
    /// Names the declarative form the violation is missing, the way <c>dca-archunit</c>'s
    /// <c>FrameworkAnnotations.describe</c> does — and says so when the preset configures none,
    /// which is the default in .NET.
    /// </summary>
    /// <summary>
    /// The output ports a unit calls that are not transactional resources — the ones whose call may
    /// leave the process. Sorted and de-duplicated for a stable message.
    /// </summary>
    private static IEnumerable<string> RemotePortsCalledBy(MethodBase unit, DcaArchitecture arch)
    {
        var markers = arch.Layout.Markers;
        var transactional = new[]
        {
            nameof(DcaMarkers.Repository), nameof(DcaMarkers.Store), nameof(DcaMarkers.DomainEventPublisher), nameof(DcaMarkers.IntegrationEventPublisher),
        };
        return IntraClassCalls.IlCalls(unit)
            .Select(m => m.DeclaringType)
            .Where(t => t is not null && markers.Is(nameof(DcaMarkers.OutputPort), t))
            .Where(t => !transactional.Any(role => markers.Is(role, t)))
            .Where(t => !arch.Layout.LocalOutputPorts.Any(local => DcaMarkers.IsAssignableToByName(t, local)))
            .Select(t => t!.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal);
    }

    private static string DescribeTransactionalAttribute(string attribute) =>
        string.IsNullOrWhiteSpace(attribute)
            ? "declarative transaction metadata (none configured)"
            : attribute;

    /// <summary>
    /// The effect of a code unit that needs a transaction, worded for the violation, or <c>null</c>
    /// when the unit neither writes through an IRepository nor publishes domain events.
    /// </summary>
    private static string? TransactionalEffectOf(MethodBase unit, DcaMarkers markers)
    {
        var saves = IntraClassCalls.Calls(unit, "SaveAsync", t => markers.Is(nameof(DcaMarkers.Repository), t));
        var deletes = IntraClassCalls.Calls(unit, "DeleteByIdAsync", t => markers.Is(nameof(DcaMarkers.Repository), t));
        var publishes = IntraClassCalls.IlCalls(unit).Any(m => m.DeclaringType is not null && markers.Is(nameof(DcaMarkers.DomainEventPublisher), m.DeclaringType));
        if (!saves && !deletes && !publishes)
        {
            return null;
        }

        var effects = new List<string>();
        if (saves)
        {
            effects.Add("saves an aggregate");
        }

        if (deletes)
        {
            effects.Add("deletes an aggregate");
        }

        if (publishes)
        {
            effects.Add("publishes domain events");
        }

        return string.Join(" and ", effects);
    }

    private static bool CallsBoundary(MethodBase unit, DcaMarkers markers) => IntraClassCalls.IlCalls(unit).Any(m => m.Name == "InTransactionAsync"
        && (markers.Is(nameof(DcaMarkers.TransactionBoundary), m.DeclaringType)
            || (m.IsStatic && markers.Is(nameof(DcaMarkers.TransactionBoundary), m.GetParameters().FirstOrDefault()?.ParameterType))));

    private static bool Transactional(MemberInfo target, string attributeName)
    {
        if (string.IsNullOrWhiteSpace(attributeName))
        {
            return false;
        }

        return target.GetCustomAttributesData().Any(a =>
        {
            for (var type = a.AttributeType; type is not null; type = type.BaseType)
            {
                if (type.FullName == attributeName)
                {
                    return true;
                }
            }

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
                    .FollowCustomPredicate(arch.InDomain, "are in the domain layer")
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
                    .FollowCustomPredicate(arch.InApplication, "are in the application layer")
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
    public static IDcaRule TransactionalUseCasesDoNotCallRemotePorts(DcaLayout layout) =>
        DcaRule.Check("DCA-USE-013", "Declaratively transactional use cases must not call remote-capable output ports",
            "A declaratively transactional use case holds a database connection for its whole run. Calling an output port"
            + " that may leave the process (another context's API, a payment provider, a mail gateway) inside it blocks"
            + " that connection for the remote round trip; under load the pool runs dry, and a rollback cannot undo the"
            + " remote effect. Awaiting the call does not help: the thread is released, the transaction and its connection"
            + " are not. Only transactional resources belong inside the boundary: IRepository, IStore,"
            + " IDomainEventPublisher, IIntegrationEventPublisher. Everything else is called before the transaction - draw"
            + " the boundary by hand with ITransactionBoundary.InTransactionAsync(...) - or after it, as a reaction to an"
            + " integration event",
            arch =>
            {
                var attribute = layout.FrameworkTypes.TransactionalAttribute;
                var violations = new List<string>();
                if (FrameworkTypes.IsSet(attribute))
                {
                    foreach (var type in arch.Types.Where(t => OperationPolicy.Operation(t, arch)))
                    {
                        var runtime = arch.RuntimeType(type)!;
                        var graph = new IntraClassCalls(runtime);
                        foreach (var unit in graph.Units)
                        {
                            if (!Transactional(runtime, attribute)
                                && !graph.EntryPointsOf(unit).Any(entry =>
                                    Transactional(entry, attribute)
                                    || !graph.ReachableThrough(entry, u => !Transactional(u, attribute)).Contains(unit)))
                            {
                                continue;
                            }

                            var remotePorts = RemotePortsCalledBy(unit, arch).ToList();
                            if (remotePorts.Count == 0)
                            {
                                continue;
                            }

                            violations.Add(
                                $"{type.FullName}.{IntraClassCalls.DisplayName(unit)} runs under {attribute} and calls "
                                + string.Join(", ", remotePorts)
                                + " inside the transaction - call it before, or draw the boundary with"
                                + " ITransactionBoundary.InTransactionAsync(...)");
                        }
                    }
                }

                DcaRule.Fail(
                    "Declaratively transactional use cases must not call remote-capable output ports",
                    violations.Distinct().ToList(),
                    "call the remote port before the transaction, or draw the boundary explicitly with"
                    + " ITransactionBoundary.InTransactionAsync(...) around the transactional part only");
            })
        .Selecting("Concrete application operations selected by IInputPort marker or configured use-case suffix, with loadable runtime types. Nothing at all while no transactional attribute is configured, which is the default: neither .NET preset names one, because ASP.NET Core has no ambient transaction attribute.")
        .Checking("Every code unit that runs under the configured transactional attribute - on the class, on itself, or on an entry point that reaches it within the class - calls no output port other than IRepository, IStore, IDomainEventPublisher, IIntegrationEventPublisher or a port the layout declares local (a hasher, a clock, an id source - DcaLayout.WithLocalOutputPorts). A use case without such an attribute (an explicit ITransactionBoundary, or none) is selected but never reported, and with no attribute configured nothing is ever reported - the rule then carries the id without checking anything, as the Java twin does where no framework annotation is on the class path. What happens inside an InTransactionAsync block is not inspected; the explicit boundary is the developer's own and its extent is visible at the call site.");

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
            var shared = application + "." + layout.SharedSegment;
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
                    || !(IsRole(arch, candidate, nameof(DcaMarkers.InputPort)) || candidate.Name.EndsWith(layout.UseCaseSuffix, StringComparison.Ordinal)))
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
            "No instance result member of any visibility - the walk binds public and non-public members - "
            + "transitively through application part records and generic wrappers exposes IAggregateRoot or "
            + "IEntity; classes and structs are checked. Domain values and read models are not traversed. A "
            + "type the loader cannot resolve is not walked.");

    private static void CheckResultsCarryNoIdentities(DcaArchitecture arch)
    {
        bool application(Type type) => arch.InLayer(type, DcaLayout.Layer.Application);
        var violations = new List<string>();
        foreach (var result in arch.Types.Where(t => t is Class or Struct)
            .Where(c => c.Namespace is not null
                && arch.InApplication(c)
                && !c.IsNested
                && c.Name.EndsWith("Result", StringComparison.Ordinal)
                && !IsRole(arch, c, nameof(DcaMarkers.Value)))
            .OrderBy(c => c.FullName, StringComparer.Ordinal))
        {
            var runtime = arch.RuntimeType(result);
            if (runtime is null)
            {
                continue;
            }

            WalkResultMembers(application, runtime, runtime.Name, new HashSet<Type>(), violations, arch.Layout.Markers);
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
    private static void WalkResultMembers(Func<Type, bool> application, Type type, string path, ISet<Type> onPath, ICollection<string> violations, DcaMarkers markers)
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

            CheckResultMemberType(application, memberType, $"{path}.{name}", onPath, violations, markers);
        }

        onPath.Remove(type);
    }

    /// <summary>
    /// One member type: an identity is a violation; an array is checked through its element type; a generic
    /// instantiation through every type argument <em>and</em> its own members (a generic part record may hide an
    /// identity next to its type parameter); a part record through its members.
    /// </summary>
    private static void CheckResultMemberType(Func<Type, bool> application, Type type, string path, ISet<Type> onPath, ICollection<string> violations, DcaMarkers markers)
    {
        var identity = IdentityMarkerOf(type, markers);
        if (identity is not null)
        {
            violations.Add($"{path} : {type.Name} ({identity})");
            return;
        }

        if (type.IsArray)
        {
            CheckResultMemberType(application, type.GetElementType()!, path, onPath, violations, markers);
            return;
        }

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                CheckResultMemberType(application, argument, path, onPath, violations, markers);
            }
        }

        if (IsPartRecord(application, type))
        {
            WalkResultMembers(application, type, $"{path} -> {PlainName(type)}", onPath, violations, markers);
        }
    }

    /// <summary>
    /// A part record: a record (class or struct) declared in an application namespace — nested in the result, next
    /// to it, or shared in <c>Application.Shared</c>. Types from other layers (value objects, read models) are values
    /// by contract and are not walked; BCL and generic parameters never are.
    /// </summary>
    private static bool IsPartRecord(Func<Type, bool> application, Type type) =>
        !type.IsGenericParameter
        && !type.IsPrimitive
        && !type.IsEnum
        && type.Namespace is not null
        && application(type)
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

    private static string? IdentityMarkerOf(Type type, DcaMarkers markers) =>
        markers.Is(nameof(DcaMarkers.AggregateRoot), type) ? SimpleName(markers.AggregateRoot)
        : markers.Is(nameof(DcaMarkers.Entity), type) ? SimpleName(markers.Entity)
        : null;

    /// <summary>The simple name of a configured marker, for a message a reader has to recognise.</summary>
    private static string SimpleName(string fullName) => fullName[(fullName.LastIndexOf('.') + 1)..];

    /// <summary>
    /// A closed outcome type in a domain namespace, or a direct case of one: a sum type the domain returns,
    /// whatever it is named. C# has no sealed hierarchy for classes; the closest honest reading is an abstract
    /// class (or record) whose instance constructors are all private or private protected — then no other
    /// assembly can derive from it, and with private ones only its nested types. A record's compiler-generated copy constructor is protected and is
    /// ignored: a type outside could derive through it only by copying an existing case, which no one does by
    /// accident. Interfaces cannot be closed in C# and are never exempt.
    /// </summary>
    private static bool IsClosedDomainOutcome(DcaArchitecture arch, IType type)
    {
        if (type.Namespace is null || !arch.InDomain(type))
        {
            return false;
        }

        var runtime = arch.RuntimeType(type);
        return runtime is not null && (IsClosed(runtime) || (runtime.BaseType is { } baseType && IsClosed(baseType)));
    }

    private static bool IsClosed(Type type)
    {
        if (!type.IsClass || !type.IsAbstract || type.IsSealed)
        {
            return false;
        }

        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(c => !IsRecordCopyConstructor(type, c))
            .ToList();
        return constructors.Count > 0 && constructors.All(c => c.IsPrivate || c.IsFamilyAndAssembly);
    }

    private static bool IsRecordCopyConstructor(Type type, ConstructorInfo constructor) =>
        constructor.GetParameters() is { Length: 1 } parameters
        && parameters[0].ParameterType == type
        && type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null;

    private static void ImmutableApplicationModels(DcaArchitecture arch, string suffix, Func<IType, bool> model)
    {
        // Selects classes and structs only: an enum is its own kind in the type model and immutable by
        // construction - its one instance field, the underlying value, is not readonly.
        var violations = arch.Types.Where(t => t is Class or Struct)
            .Where(c => c.Namespace is not null
                && arch.InApplication(c)
                && model(c)
                && !TacticalPatternRules.IsImmutableShape(c))
            .Select(c => $"{c.FullName} does not have immutable instance state")
            .ToList();
        DcaRule.Fail(
            $"Use case {suffix} models should be immutable (sealed or records)",
            violations,
            $"declare *{suffix} types with immutable instance state");
    }

    /// <summary>The input ports (interfaces and classes assignable to the role) a class depends on, as runtime types.</summary>
    private static IEnumerable<Type> InputPortsOf(DcaArchitecture arch, Class adapter)
    {
        var found = new HashSet<Type>();
        foreach (var target in adapter.Dependencies.Select(d => d.Target))
        {
            if (!target.IsGenericParameter && IsRole(arch, target, nameof(DcaMarkers.InputPort)) && arch.RuntimeType(target) is { } port)
            {
                found.Add(port);
            }
        }

        var runtime = arch.RuntimeType(adapter);
        if (runtime is not null)
        {
            foreach (var parameter in runtime
                .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType)
                .Where(t => arch.Layout.Markers.Is(nameof(DcaMarkers.InputPort), t)))
            {
                found.Add(parameter);
            }
        }

        return found;
    }

    /// <summary>The input types of an input port: an interface's use-case argument and the parameters of its input-port methods, or a class's <see cref="InputTypesOf"/>.</summary>
    private static IEnumerable<Type> PortInputs(Type port, DcaMarkers markers)
    {
        if (!port.IsInterface)
        {
            return InputTypesOf(port, markers);
        }

        var contracts = port.GetInterfaces().Append(port).ToList();
        return contracts
            .Where(c => c.IsGenericType && DcaMarkers.IsRole(c.GetGenericTypeDefinition(), markers.UseCase))
            .Select(c => c.GetGenericArguments()[0])
            .Concat(contracts
                .Where(c => markers.Is(nameof(DcaMarkers.InputPort), c))
                .SelectMany(c => c.GetMethods())
                .SelectMany(m => m.GetParameters())
                .Select(p => p.ParameterType));
    }

    /// <summary>
    /// The feature a use-case namespace belongs to: the segment below the module's application namespace when the
    /// namespace sits at <c>Application.&lt;Feature&gt;.&lt;UseCase&gt;</c> once the configured operation-container
    /// segments are removed; <c>null</c> for a flat namespace, <c>Application.Shared</c> and another module.
    /// </summary>
    private static string? FeatureOf(string ns, string root, DcaLayout layout)
    {
        var application = root + "." + layout.ApplicationSegment;
        if (!ns.StartsWith(application + ".", StringComparison.Ordinal))
        {
            return null;
        }

        var segments = ns.Substring(application.Length + 1).Split('.').Where(segment => !layout.OperationContainers.Contains(segment)).ToList();
        return segments.Count == 2 && segments[0] != layout.SharedSegment ? segments[0] : null;
    }

    /// <summary>A port method name that writes: a write verb followed by the end, an upper-case letter or a digit.</summary>
    private static readonly System.Text.RegularExpressions.Regex WriteVerb = new(
        "^(Save|Delete|Remove|Add|Insert|Update|Upsert|Put|Store|Write|Create|Append|Archive|Purge|Clear|Set|Mark)(?=$|[A-Z0-9])",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>What a call does that a query must not, worded for the violation, or <c>null</c>.</summary>
    private static string? WriteEffectOf(MethodBase call, DcaMarkers markers)
    {
        var owner = call.DeclaringType;
        if (owner is null)
        {
            return null;
        }

        if (markers.Is(nameof(DcaMarkers.DomainEventPublisher), owner) || markers.Is(nameof(DcaMarkers.IntegrationEventPublisher), owner)
            || (call.IsStatic && call.GetParameters().FirstOrDefault()?.ParameterType is { } first
                && (markers.Is(nameof(DcaMarkers.DomainEventPublisher), first) || markers.Is(nameof(DcaMarkers.IntegrationEventPublisher), first))))
        {
            return "publishes events";
        }

        var port = markers.Is(nameof(DcaMarkers.Repository), owner) ? "repository"
            : markers.Is(nameof(DcaMarkers.Store), owner) ? "store" : null;
        return port is not null && WriteVerb.IsMatch(call.Name) ? $"writes through a {port}" : null;
    }

    /// <summary>The input types of an operation: the input argument of its use-case role, and the parameters of the methods implementing its input ports.</summary>
    private static IEnumerable<Type> InputTypesOf(Type runtime, DcaMarkers markers)
    {
        foreach (var contract in runtime.GetInterfaces())
        {
            if (contract.IsGenericType && DcaMarkers.IsRole(contract.GetGenericTypeDefinition(), markers.UseCase))
            {
                yield return contract.GetGenericArguments()[0];
            }

            if (runtime.IsInterface || !markers.Is(nameof(DcaMarkers.InputPort), contract))
            {
                continue;
            }

            foreach (var method in runtime.GetInterfaceMap(contract).TargetMethods)
            {
                foreach (var parameter in method.GetParameters())
                {
                    yield return parameter.ParameterType;
                }
            }
        }
    }

    /// <summary>
    /// Assignability via the ArchUnitNET model (implemented interfaces, base classes) with a reflection
    /// fallback for types whose interface closure the loader did not resolve.
    /// </summary>
    private static bool IsRole(DcaArchitecture arch, IType type, string role) =>
        arch.Layout.Markers.Is(role, type) || arch.Layout.Markers.Is(role, arch.RuntimeType(type));
}
