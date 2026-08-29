using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ArchUnitNET.Domain;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using Type = System.Type;

namespace DomainCentric.ArchRules.Rules;

/// <summary>
/// Rules that only exist in the .NET edition of Domain-Centric Architecture (<c>DCA-NET-*</c>): they
/// pin down how the doctrine maps onto C# idioms — a synchronous domain, async-only ports with the
/// <c>Async</c> suffix, one <c>ExecuteAsync</c> per use case, records for values and identifiers.
/// They have no Java counterpart and therefore no entry in the shared catalog of ported rules.
/// </summary>
public sealed class DotnetRules : IDcaRuleSet
{
    /// <summary>Java rule ids without a .NET reading — none, this set is .NET-native.</summary>
    public static readonly IReadOnlyDictionary<string, string> NotApplicable = new Dictionary<string, string>();

    public DotnetRules(DcaLayout layout)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Rules = new IDcaRule[]
        {
            DomainLayerMustStaySynchronous(),
            PortMethodsReturningTaskMustEndWithAsync(),
            UseCasesMustExposeExactlyOneExecuteAsync(),
            ValueObjectsShouldBeRecords(),
            IdentifiersShouldBeReadonlyRecordStructs(),
        };
    }

    public string Name => "dotnet";

    public IReadOnlyList<IDcaRule> Rules { get; }

    /// <summary>The layout this rule set was built for.</summary>
    public DcaLayout Layout { get; }

    // ---------------------------------------------------------------------------------------------
    // Rules
    // ---------------------------------------------------------------------------------------------

    /// <summary>DCA-NET-001: no <c>Task</c>, <c>ValueTask</c> or <c>CancellationToken</c> in the domain layer.</summary>
    public static IDcaRule DomainLayerMustStaySynchronous() =>
        DcaRule.Check(
            "DCA-NET-001",
            "Domain layer must stay synchronous",
            "Async is an I/O concern of ports and adapters; a synchronous domain model stays testable, deterministic and free of sync-over-async hazards",
            arch =>
            {
                var domain = arch.AllDomainPatternsWithSharedKernel().Select(p => new Regex(p)).ToList();
                var violations = new List<string>();
                foreach (var type in arch.Types.Where(t => domain.Any(r => r.IsMatch(t.Namespace.FullName))))
                {
                    var async = type.Dependencies
                        .Select(d => d.Target.FullName)
                        .Where(IsAsyncType)
                        .Distinct()
                        .OrderBy(n => n, StringComparer.Ordinal)
                        .ToList();
                    if (async.Count > 0)
                    {
                        violations.Add($"{type.FullName} depends on {string.Join(", ", async)}");
                    }
                }

                DcaRule.Fail(
                    "Domain layer must stay synchronous\nbecause async is an I/O concern of ports and adapters",
                    violations,
                    "Move the awaiting code into a use case or adapter and pass plain values into the domain.");
            });

    /// <summary>DCA-NET-002: awaitable port methods end with <c>Async</c>, and only those.</summary>
    public static IDcaRule PortMethodsReturningTaskMustEndWithAsync() =>
        DcaRule.Check(
            "DCA-NET-002",
            "Port methods returning Task must end with Async",
            "The Async suffix is the .NET convention that tells callers a method is awaitable; ports are the contract other layers program against",
            arch =>
            {
                var violations = new List<string>();
                foreach (var port in arch.Interfaces)
                {
                    var runtime = arch.RuntimeType(port);
                    if (runtime is null || !(IsPort(runtime, typeof(IInputPort)) || IsPort(runtime, typeof(IOutputPort))))
                    {
                        continue;
                    }

                    foreach (var method in DeclaredMethods(runtime))
                    {
                        var awaitable = IsAsyncReturnType(method.ReturnType);
                        var suffixed = method.Name.EndsWith("Async", StringComparison.Ordinal);
                        if (awaitable && !suffixed)
                        {
                            violations.Add($"{runtime.FullName}.{method.Name} returns {method.ReturnType.Name} but does not end with Async");
                        }
                        else if (!awaitable && suffixed)
                        {
                            violations.Add($"{runtime.FullName}.{method.Name} ends with Async but returns {method.ReturnType.Name}");
                        }
                    }
                }

                DcaRule.Fail(
                    "Port methods returning Task must end with Async\nbecause the Async suffix tells callers a method is awaitable",
                    violations,
                    "Name every Task/ValueTask-returning port method *Async and make every *Async method return Task or ValueTask.");
            });

    /// <summary>DCA-NET-003: a use case has exactly one public <c>ExecuteAsync(…, CancellationToken)</c> returning <c>Task&lt;T&gt;</c>.</summary>
    public static IDcaRule UseCasesMustExposeExactlyOneExecuteAsync() =>
        DcaRule.Check(
            "DCA-NET-003",
            "Use cases must expose exactly one ExecuteAsync",
            "One use case, one entry point: the input port is the only way in, and a cancellation token lets the host stop long-running work",
            arch =>
            {
                var violations = new List<string>();
                foreach (var useCase in arch.Classes)
                {
                    var runtime = arch.RuntimeType(useCase);
                    if (runtime is null || runtime.IsAbstract || !ImplementsUseCase(runtime))
                    {
                        continue;
                    }

                    var executes = DeclaredMethods(runtime).Where(m => m.Name == "ExecuteAsync").ToList();
                    if (executes.Count != 1)
                    {
                        violations.Add($"{runtime.FullName} declares {executes.Count} public ExecuteAsync methods");
                        continue;
                    }

                    var execute = executes[0];
                    var returnsTaskOfT = execute.ReturnType.IsGenericType && execute.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
                    if (!returnsTaskOfT)
                    {
                        violations.Add($"{runtime.FullName}.ExecuteAsync must return Task<T> but returns {execute.ReturnType.Name}");
                    }

                    var last = execute.GetParameters().LastOrDefault();
                    if (last is null || last.ParameterType != typeof(CancellationToken))
                    {
                        violations.Add($"{runtime.FullName}.ExecuteAsync must take a CancellationToken as its last parameter");
                    }
                }

                DcaRule.Fail(
                    "Use cases must expose exactly one ExecuteAsync\nbecause the input port is the only way in",
                    violations,
                    "Implement IUseCase<TInput, TOutput>.ExecuteAsync(input, cancellationToken) once and keep every other member non-public.");
            });

    /// <summary>DCA-NET-004: value objects are records or (record) structs.</summary>
    public static IDcaRule ValueObjectsShouldBeRecords() =>
        DcaRule.Check(
            "DCA-NET-004",
            "Value objects should be records or readonly record structs",
            "Records give attribute-based equality, immutability by default and with-expressions — the C# way to write a Value Object",
            arch =>
            {
                var domain = arch.AllDomainPatternsWithSharedKernel().Select(p => new Regex(p)).ToList();
                var violations = new List<string>();
                foreach (var type in arch.Types.Where(t => domain.Any(r => r.IsMatch(t.Namespace.FullName))))
                {
                    var runtime = arch.RuntimeType(type);
                    if (runtime is null || runtime.IsInterface || runtime.IsAbstract || !typeof(IValue).IsAssignableFrom(runtime))
                    {
                        continue;
                    }

                    if (!(IsRecord(type, runtime) || runtime.IsValueType))
                    {
                        violations.Add($"{runtime.FullName} implements IValue but is a plain class");
                    }
                }

                DcaRule.Fail(
                    "Value objects should be records or readonly record structs\nbecause records are the C# way to write a Value Object",
                    violations,
                    "Declare the value object as `public sealed record X(...)` or `public readonly record struct X(...)`.");
            });

    /// <summary>DCA-NET-005: identifiers are record structs.</summary>
    public static IDcaRule IdentifiersShouldBeReadonlyRecordStructs() =>
        DcaRule.Check(
            "DCA-NET-005",
            "Identifiers should be readonly record structs",
            "A strongly typed identifier as a readonly record struct costs no allocation and cannot be confused with a raw Guid or string",
            arch =>
            {
                var violations = new List<string>();
                foreach (var type in arch.Types)
                {
                    var runtime = arch.RuntimeType(type);
                    if (runtime is null || runtime.IsInterface || runtime.IsAbstract || !typeof(IId).IsAssignableFrom(runtime))
                    {
                        continue;
                    }

                    if (!runtime.IsValueType)
                    {
                        violations.Add($"{runtime.FullName} implements IId but is a reference type");
                    }
                    else if (!IsRecord(type, runtime))
                    {
                        violations.Add($"{runtime.FullName} implements IId but is a plain struct, not a record struct");
                    }
                }

                DcaRule.Fail(
                    "Identifiers should be readonly record structs\nbecause a readonly record struct costs no allocation and cannot be confused with a raw Guid or string",
                    violations,
                    "Declare the identifier as `public readonly record struct XId(Guid Value) : IId`.");
            });

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    private static bool IsAsyncType(string fullName) =>
        fullName == "System.Threading.CancellationToken" ||
        fullName == "System.Threading.Tasks.Task" ||
        fullName.StartsWith("System.Threading.Tasks.Task`", StringComparison.Ordinal) ||
        fullName == "System.Threading.Tasks.ValueTask" ||
        fullName.StartsWith("System.Threading.Tasks.ValueTask`", StringComparison.Ordinal);

    private static bool IsAsyncReturnType(Type type) =>
        type == typeof(Task) || type == typeof(ValueTask) ||
        (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Task<>) || type.GetGenericTypeDefinition() == typeof(ValueTask<>)));

    private static bool IsPort(Type type, Type marker) => type.IsInterface && marker.IsAssignableFrom(type);

    private static bool ImplementsUseCase(Type type) =>
        type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IUseCase<,>));

    /// <summary>Public instance methods declared on the type itself, accessors and operators excluded.</summary>
    private static IEnumerable<MethodInfo> DeclaredMethods(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => !m.IsSpecialName);

    /// <summary>
    /// Records and record structs: ArchUnitNET flags record classes; for structs the compiler-generated
    /// <c>PrintMembers</c> method is the tell-tale sign.
    /// </summary>
    private static bool IsRecord(IType type, Type runtime) =>
        (type is Class c && c.IsRecord == true) ||
        runtime.GetMethod("PrintMembers", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, new[] { typeof(System.Text.StringBuilder) }, null) is not null;
}
