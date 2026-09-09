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
            ApplicationLayerMustNotUsePersistenceFrameworks(),
        };
    }

    public string Name => "dotnet";

    public IReadOnlyList<IDcaRule> Rules { get; }

    /// <summary>The layout this rule set was built for.</summary>
    public DcaLayout Layout { get; }

    // ---------------------------------------------------------------------------------------------
    // Rules
    // ---------------------------------------------------------------------------------------------

    /// <summary>DCA-NET-006: no EF Core, ADO.NET or System.Transactions types in the application layer.</summary>
    public static IDcaRule ApplicationLayerMustNotUsePersistenceFrameworks() =>
        DcaRule.Check(
            "DCA-NET-006",
            "Application layer must not use persistence or transaction frameworks",
            "The transaction boundary of a use case is drawn by a decorator around IUseCase or by ITransactionBoundary (an application-layer execution abstraction implemented in infrastructure), never by DbContext, SaveChanges, TransactionScope or IDbTransaction in the use case itself. Framework types in the application layer bind use cases to one persistence technology and hide where the boundary is; the ITransactionBoundary implementation is the single place that knows how to open and commit a transaction",
            arch =>
            {
                var application = arch.AllApplicationPatterns().Select(p => new Regex(p)).ToList();
                var violations = new List<string>();
                foreach (var type in arch.Types.Where(t => application.Any(r => r.IsMatch(t.Namespace.FullName))))
                {
                    var frameworks = type.Dependencies
                        .Select(d => d.Target.FullName)
                        .Where(IsPersistenceFrameworkType)
                        .Distinct()
                        .OrderBy(n => n, StringComparer.Ordinal)
                        .ToList();
                    if (frameworks.Count > 0)
                    {
                        violations.Add($"{type.FullName} depends on {string.Join(", ", frameworks)}");
                    }
                }

                DcaRule.Fail(
                    "Application layer must not use persistence or transaction frameworks\nbecause the transaction boundary belongs to a decorator or ITransactionBoundary",
                    violations,
                    "Inject ITransactionBoundary (or let the composition root decorate the use case) and move the framework call into infrastructure.");
            })
        .Selecting(
            "Types in <module>.Application of every module root.")
        .Checking(
            "No dependency on a type whose full name starts with Microsoft.EntityFrameworkCore,"
                + " System.Transactions, System.Data, Dapper, NHibernate or MongoDB.Driver. Only"
                + " these six namespace prefixes are checked - another persistence library is not"
                + " reported, and the domain and adapter layers are not selected. An empty selection"
                + " passes.");

    private static readonly string[] PersistenceFrameworkNamespaces =
    {
        "Microsoft.EntityFrameworkCore.",
        "System.Transactions.",
        "System.Data.",
        "Dapper.",
        "NHibernate.",
        "MongoDB.Driver.",
    };

    private static bool IsPersistenceFrameworkType(string fullName) =>
        PersistenceFrameworkNamespaces.Any(ns => fullName.StartsWith(ns, StringComparison.Ordinal));

    /// <summary>DCA-NET-001: no <c>Task</c>, <c>ValueTask</c> or <c>CancellationToken</c> in the domain layer.</summary>
    public static IDcaRule DomainLayerMustStaySynchronous() =>
        DcaRule.Check(
            "DCA-NET-001",
            "Domain layer must stay synchronous",
            "Async is an I/O concern of ports and adapters; a synchronous domain model stays testable, deterministic and free of sync-over-async hazards",
            arch =>
            {
                var domain = arch.AllDomainPatterns().Select(p => new Regex(p)).ToList();
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
            })
        .Selecting(
            "Types in <module>.Domain of every module root.")
        .Checking(
            "No dependency on Task, Task<T>, ValueTask, ValueTask<T> or CancellationToken -"
                + " in a signature, a field or a method body. Other awaitables and IAsyncEnumerable<T>"
                + " are not checked. An empty selection passes.");

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
            })
        .Selecting(
            "Interfaces under the root namespace that are assignable to IInputPort or"
                + " IOutputPort and whose runtime type is in the loaded assemblies.")
        .Checking(
            "Every public method declared on the interface itself (inherited members, property"
                + " accessors and operators excluded) returns Task, Task<T>, ValueTask or ValueTask<T>"
                + " exactly when its name ends with Async. Both directions are reported: an awaitable"
                + " method without the suffix and a suffixed method that returns something else. An"
                + " empty selection passes.");

    /// <summary>DCA-NET-003: validate the asynchronous generic input contract through interface maps.</summary>
    public static IDcaRule UseCasesMustExposeExactlyOneExecuteAsync() =>
        DcaRule.Check("DCA-NET-003", "Use cases implement IUseCase<TIn,TOut>.ExecuteAsync(input, CancellationToken) returning Task<T>",
            "The generic asynchronous input contract supports host cancellation",
            arch => {
                var violations = new List<string>();
                foreach (var type in arch.Classes) {
                    var runtime = arch.RuntimeType(type);
                    if (runtime is null || runtime.IsAbstract || !(OperationPolicy.Operation(type, arch) || ImplementsUseCase(runtime))) continue;
                    var contracts = runtime.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IUseCase<,>)).ToArray();
                    if (contracts.Length == 0) violations.Add($"{type.FullName} must implement IUseCase<TIn,TOut>; a plain Task ExecuteAsync does not satisfy the contract");
                    foreach (var contract in contracts) {
                        var map = runtime.GetInterfaceMap(contract);
                        foreach (var method in map.TargetMethods) {
                            var parameters = method.GetParameters();
                            if (!method.ReturnType.IsGenericType || method.ReturnType.GetGenericTypeDefinition() != typeof(Task<>)
                                || parameters.Length != 2 || parameters[1].ParameterType != typeof(CancellationToken))
                                violations.Add($"{type.FullName}.{method.Name} must accept input and CancellationToken and return Task<T>");
                        }
                    }
                }
                DcaRule.Fail("Use cases implement the generic asynchronous input contract", violations);
            })
        .Selecting("Concrete application operations selected by marker or suffix, plus concrete IUseCase<TIn,TOut> implementations anywhere under scan, with loadable runtime types.")
        .Checking("An IUseCase<TIn,TOut> interface map supplies ExecuteAsync(input, CancellationToken) returning Task<T>. Explicit, inherited and ordinary implementations pass. A plain Task method without the generic contract fails; other public members are checked by USE-017, not counted here.");

    /// <summary>DCA-NET-004: value objects are records or (record) structs.</summary>
    public static IDcaRule ValueObjectsShouldBeRecords() =>
        DcaRule.Check(
            "DCA-NET-004",
            "Struct value objects must be readonly",
            "Records give attribute-based equality, immutability by default and with-expressions — the C# way to write a Value Object",
            arch =>
            {
                var domain = arch.AllDomainPatterns().Select(p => new Regex(p)).ToList();
                var violations = new List<string>();
                foreach (var type in arch.Types.Where(t => domain.Any(r => r.IsMatch(t.Namespace.FullName))))
                {
                    var runtime = arch.RuntimeType(type);
                    if (runtime is null || runtime.IsInterface || runtime.IsAbstract || !typeof(IValue).IsAssignableFrom(runtime))
                    {
                        continue;
                    }

                    if (runtime.IsValueType && !runtime.IsDefined(typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute), false))
                    {
                        violations.Add($"{runtime.FullName} implements IValue but is a mutable struct");
                    }
                }

                DcaRule.Fail(
                    "Struct value objects must be readonly\nbecause records are the C# way to write a Value Object",
                    violations,
                    "Declare the value object as `public sealed record X(...)` or `public readonly record struct X(...)`.");
            })
        .Selecting(
            "Non-interface, non-abstract types in <module>.Domain of every module root that"
                + " are assignable to IValue and whose runtime type is in the loaded assemblies.")
        .Checking(
            "Struct values must carry the readonly modifier. Classes are governed by TAC-009/010/012; equality is checked by TAC-012. An IValue outside a domain namespace is not selected." );

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
                    else if (!IsRecord(type, runtime) || !runtime.IsDefined(typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute), false))
                    {
                        violations.Add($"{runtime.FullName} implements IId but is a plain struct, not a record struct");
                    }
                }

                DcaRule.Fail(
                    "Identifiers should be readonly record structs\nbecause a readonly record struct costs no allocation and cannot be confused with a raw Guid or string",
                    violations,
                    "Declare the identifier as `public readonly record struct XId(Guid Value) : IId`.");
            })
        .Selecting(
            "Non-interface, non-abstract types anywhere under the root namespace that are"
                + " assignable to IId and whose runtime type is in the loaded assemblies.")
        .Checking(
            "The type must be a readonly record struct. Reference types, plain structs and mutable record structs fail." );

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
