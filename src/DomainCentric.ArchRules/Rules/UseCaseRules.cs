using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Dependencies;
using ArchUnitNET.Domain.Extensions;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
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
    public static readonly IReadOnlyDictionary<string, string> NotApplicable = new Dictionary<string, string>
    {
        ["DCA-USE-013"] = "Guards against remote-capable output ports called inside a @Transactional use case. .NET has no declarative transaction metadata on use cases — the boundary is a decorator or an explicit ITransactionBoundary.InTransactionAsync — so the rule has nothing to anchor on; DCA-NET-006 keeps transaction and persistence frameworks out of the application layer instead",
        ["DCA-USE-012"] = "Guards Spring's after-commit relay (@TransactionalEventListener / @ApplicationModuleListener), which is skipped silently without an active transaction. .NET has no ambient transaction attribute on use cases; after-save delivery is the job of the integration-event outbox adapter, not of the use case",
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
            UseCasePackagesUseOneDepth(layout),
            ResultsMustNotExposeAggregatesOrEntities(layout),
        };
    }

    public string Name => "usecase";

    public IReadOnlyList<IDcaRule> Rules { get; }

    /// <summary>The layout this rule set was built for.</summary>
    public DcaLayout Layout { get; }

    public static IDcaRule BaseInputPortResidesInBuildingBlocks(DcaLayout layout) =>
        DcaRule.Of(
            "DCA-USE-001",
            "Base IInputPort interface must be in the building-blocks port in namespace",
            "Base IInputPort interface defines the generic contract for all use cases (Hexagonal Architecture)",
            arch =>
                Interfaces()
                    .That()
                    .HaveNameMatching("^I?InputPort$")
                    .Should()
                    .ResideInNamespaceMatching(DcaLayout.Exactly(DcaLayout.BuildingBlocksPortsInNamespace)));

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
                    .ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllApplicationPatterns())));

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
                    .ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllApplicationPatterns())));

    public static IDcaRule CommandsAreImmutable(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-USE-004",
            "Use Case Commands should be immutable (sealed or records)",
            "Use case commands should be immutable (value objects)",
            arch => ImmutableApplicationModels(arch, "Command"));

    public static IDcaRule QueriesAreImmutable(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-USE-005",
            "Use Case Queries should be immutable (sealed or records)",
            "Use case queries should be immutable (value objects)",
            arch => ImmutableApplicationModels(arch, "Query"));

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
                    .ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllApplicationPatterns())));

    public static IDcaRule ResultsAreImmutable(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-USE-007",
            "Use Case Result Models should be immutable (sealed or records)",
            "Use case result models should be immutable (value objects)",
            arch => ImmutableApplicationModels(arch, "Result"));

    public static IDcaRule ResponsesResideInIncomingAdapters(DcaLayout layout) =>
        // Matched by pattern: every incoming adapter, in any context or none, including the shared
        // kernel's adapter where cross-cutting Response classes typically live.
        DcaRule.Of(
            "DCA-USE-008",
            "HTTP Response Models must end with 'Response' and reside in adapter incoming namespace",
            "HTTP response models should be in adapter incoming layer",
            arch =>
                Types()
                    .That()
                    .HaveNameEndingWith("Response")
                    .And()
                    .ResideInNamespaceMatching(DcaLayout.Below(layout.RootNamespace))
                    .Should()
                    .ResideInNamespaceMatching(layout.IncomingAdapterPattern));

    public static IDcaRule UseCasesPublishDomainEventsAfterSaving(DcaLayout layout) =>
        DcaRule.Check(
            "DCA-USE-009",
            "Use cases that save an aggregate must publish its domain events",
            "A saved aggregate must not keep its events: unpublished, they are lost, and stored on the"
                + " instance they may later be published out of context. Publishing belongs after the"
                + " save, in the use case that owns the unit of work - even when the action raised no"
                + " event",
            arch =>
            {
                var violations = arch.Classes
                    .Where(c => c.Namespace is not null
                        && Matches(c.Namespace.FullName, DcaLayout.AnyOf(arch.AllApplicationPatterns()))
                        && c.Name.EndsWith(arch.Layout.UseCaseSuffix, StringComparison.Ordinal))
                    .Where(c => Calls(arch, c, "SaveAsync", typeof(IRepository))
                        && !Calls(arch, c, "PublishAndClearEventsAsync", typeof(IDomainEventPublisher)))
                    .Select(c => $"{c.FullName} saves an aggregate without publishing its domain events")
                    .ToList();
                DcaRule.Fail(
                    "Use cases that save an aggregate must publish its domain events",
                    violations,
                    "call IDomainEventPublisher.PublishAndClearEventsAsync(aggregate) after IRepository.SaveAsync(aggregate)");
            });

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
                    .HaveNameEndingWith("Dto"));

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
                    .HaveNameEndingWith("Dto"));

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
            arch => CheckUseCaseDepth(arch, layout));

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
                    || !candidate.Name.EndsWith(layout.UseCaseSuffix, StringComparison.Ordinal))
                {
                    continue;
                }

                var depth = ns == application ? 0 : ns.Substring(application.Length + 1).Split('.').Length;
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
            CheckResultsCarryNoIdentities);

    private static void CheckResultsCarryNoIdentities(DcaArchitecture arch)
    {
        var application = DcaLayout.AnyOf(arch.AllApplicationPatterns());
        var violations = new List<string>();
        foreach (var result in arch.Classes
            .Where(c => c.Namespace is not null
                && Matches(c.Namespace.FullName, application)
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

    private static void WalkResultMembers(string application, Type type, string path, ISet<Type> visited, ICollection<string> violations)
    {
        if (!visited.Add(type))
        {
            return;
        }

        const BindingFlags members = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        var memberTypes = type.GetProperties(members).Select(p => (p.Name, p.PropertyType))
            .Concat(type.GetFields(members).Select(f => (f.Name, f.FieldType)));
        foreach (var (name, memberType) in memberTypes)
        {
            if (name == "EqualityContract")
            {
                continue;
            }

            CheckResultMemberType(application, memberType, $"{path}.{name}", visited, violations);
        }
    }

    /// <summary>
    /// One member type: an identity is a violation; an array is checked through its element type; a generic
    /// instantiation through every type argument <em>and</em> its own members (a generic part record may hide an
    /// identity next to its type parameter); a part record through its members.
    /// </summary>
    private static void CheckResultMemberType(string application, Type type, string path, ISet<Type> visited, ICollection<string> violations)
    {
        var identity = IdentityMarkerOf(type);
        if (identity is not null)
        {
            violations.Add($"{path} : {type.Name} ({identity})");
            return;
        }

        if (type.IsArray)
        {
            CheckResultMemberType(application, type.GetElementType()!, path, visited, violations);
            return;
        }

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                CheckResultMemberType(application, argument, path, visited, violations);
            }
        }

        if (IsPartRecord(application, type))
        {
            WalkResultMembers(application, type, $"{path} -> {PlainName(type)}", visited, violations);
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
        var violations = arch.Classes
            .Where(c => c.Namespace is not null
                && Matches(c.Namespace.FullName, DcaLayout.AnyOf(arch.AllApplicationPatterns()))
                && c.Name.EndsWith(suffix, StringComparison.Ordinal)
                && c.IsRecord != true
                && c.IsSealed != true)
            .Select(c => $"{c.FullName} is neither sealed nor a record")
            .ToList();
        DcaRule.Fail(
            $"Use case {suffix} models should be immutable (sealed or records)",
            violations,
            $"declare *{suffix} types as records or sealed classes");
    }

    private static bool Matches(string ns, string pattern) =>
        System.Text.RegularExpressions.Regex.IsMatch(ns, pattern);

    /// <summary>
    /// Whether <paramref name="cls"/> calls a method named <paramref name="method"/> on a type assignable to
    /// <paramref name="marker"/>. Calls made inside <c>async</c> methods live in compiler-generated nested
    /// state-machine types that ArchUnitNET does not load, so the IL of the runtime type and its nested
    /// types is scanned by reflection as well.
    /// </summary>
    private static bool Calls(DcaArchitecture arch, Class cls, string method, Type marker)
    {
        var inModel = cls.Dependencies
            .OfType<MethodCallDependency>()
            .Any(d => MethodName(d.TargetMember.Name) == method && IsAssignableTo(arch, d.TargetMember.DeclaringType, marker));
        if (inModel)
        {
            return true;
        }

        var runtime = arch.RuntimeType(cls);
        return runtime is not null && CalledMethods(runtime).Any(m => m.Name == method && m.DeclaringType is not null && marker.IsAssignableFrom(m.DeclaringType));
    }

    /// <summary>All methods called from the IL of <paramref name="type"/> and its nested types (recursively).</summary>
    private static IEnumerable<MethodBase> CalledMethods(Type type)
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        foreach (var method in type.GetMethods(all).Cast<MethodBase>().Concat(type.GetConstructors(all)))
        {
            foreach (var called in IlCalls(method))
            {
                yield return called;
            }
        }

        foreach (var nested in type.GetNestedTypes(all))
        {
            foreach (var called in CalledMethods(nested))
            {
                yield return called;
            }
        }
    }

    private static readonly Lazy<IReadOnlyDictionary<short, OpCode>> OpCodeTable = new(() =>
        typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (OpCode)f.GetValue(null)!)
            .ToDictionary(o => o.Value, o => o));

    /// <summary>Walks the IL of one method and resolves the targets of call/callvirt/newobj instructions.</summary>
    private static IEnumerable<MethodBase> IlCalls(MethodBase method)
    {
        byte[]? il;
        try
        {
            il = method.GetMethodBody()?.GetILAsByteArray();
        }
        catch (InvalidOperationException)
        {
            il = null;
        }

        if (il is null)
        {
            yield break;
        }

        var typeArgs = method.DeclaringType is { IsGenericType: true } dt ? dt.GetGenericArguments() : null;
        var methodArgs = method is MethodInfo { IsGenericMethod: true } mi ? mi.GetGenericArguments() : null;
        var position = 0;
        while (position < il.Length)
        {
            short code = il[position++];
            if (code == 0xFE)
            {
                code = (short)(0xFE00 | il[position++]);
            }

            if (!OpCodeTable.Value.TryGetValue(code, out var opCode))
            {
                yield break; // unknown opcode — stop scanning this body rather than misreading operands
            }

            MethodBase? target = null;
            switch (opCode.OperandType)
            {
                case OperandType.InlineMethod:
                    var token = BitConverter.ToInt32(il, position);
                    position += 4;
                    try
                    {
                        target = method.Module.ResolveMethod(token, typeArgs, methodArgs);
                    }
                    catch (ArgumentException)
                    {
                        target = null;
                    }

                    break;
                case OperandType.InlineSwitch:
                    var count = BitConverter.ToInt32(il, position);
                    position += 4 + (4 * count);
                    break;
                case OperandType.InlineNone:
                    break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    position += 1;
                    break;
                case OperandType.InlineVar:
                    position += 2;
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    position += 8;
                    break;
                default:
                    position += 4;
                    break;
            }

            if (target is not null)
            {
                yield return target;
            }
        }
    }

    private static string MethodName(string memberName)
    {
        var paren = memberName.IndexOf('(');
        var plain = paren >= 0 ? memberName.Substring(0, paren) : memberName;
        var dot = plain.LastIndexOf('.');
        return dot >= 0 ? plain.Substring(dot + 1) : plain;
    }

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
