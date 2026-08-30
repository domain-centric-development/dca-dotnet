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
/// response models, domain-event publication after saving, and DTO-free inner layers.
/// </summary>
public sealed class UseCaseRules : IDcaRuleSet
{
    /// <summary>Java rules of this set that have no .NET counterpart (id → reason).</summary>
    public static readonly IReadOnlyDictionary<string, string> NotApplicable = new Dictionary<string, string>
    {
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
                    .ResideInNamespaceMatching(layout.ApplicationPattern));

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
                    .ResideInNamespaceMatching(layout.ApplicationPattern));

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
                    .ResideInNamespaceMatching(layout.ApplicationPattern));

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
                        && Matches(c.Namespace.FullName, arch.Layout.ApplicationPattern)
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
                    .ResideInNamespaceMatching(layout.DomainPattern)
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
                    .ResideInNamespaceMatching(layout.ApplicationPattern)
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
    private static void ImmutableApplicationModels(DcaArchitecture arch, string suffix)
    {
        var violations = arch.Classes
            .Where(c => c.Namespace is not null
                && Matches(c.Namespace.FullName, arch.Layout.ApplicationPattern)
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
