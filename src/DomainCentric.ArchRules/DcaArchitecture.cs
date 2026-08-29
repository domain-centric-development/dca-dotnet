using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using Assembly = System.Reflection.Assembly;
using Attribute = System.Attribute;
using Type = System.Type;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DomainCentric.ArchRules;

/// <summary>
/// The types under test together with the <see cref="DcaLayout"/> that describes them, plus the
/// discovery helpers every DCA rule builds on: which namespaces are bounded contexts, where the shared
/// kernel lives, which context-level attributes a namespace carries.
/// </summary>
/// <remarks>
/// <para>Create one instance per test run and pass it to every rule — the ArchUnitNET import and the
/// context discovery are cached.</para>
/// <para>Context-level declarations (<c>[BoundedContext]</c>, <c>[SharedKernel]</c>, <c>[Upstream]</c>, …)
/// are read with reflection from a <em>marker class</em> that resides directly in the context's root
/// namespace — the .NET stand-in for Java's <c>package-info</c>. Therefore the assemblies passed to
/// <see cref="Load(DcaLayout, Assembly[])"/> must be the loaded runtime assemblies.</para>
/// </remarks>
public sealed class DcaArchitecture
{
    private readonly IReadOnlyList<Assembly> _assemblies;
    private IReadOnlyDictionary<string, BoundedContextAttribute>? _boundedContexts;
    private string? _sharedKernelNamespace;
    private bool _sharedKernelResolved;
    private IReadOnlyList<Type>? _rootTypes;

    private DcaArchitecture(DcaLayout layout, Architecture architecture, IReadOnlyList<Assembly> assemblies)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Architecture = architecture ?? throw new ArgumentNullException(nameof(architecture));
        _assemblies = assemblies;
    }

    /// <summary>
    /// Imports every type of the given assemblies that resides in the layout's root namespace (or
    /// below). Pass the production assemblies of the application, e.g.
    /// <c>typeof(Program).Assembly</c> or one assembly per bounded context.
    /// </summary>
    /// <remarks>
    /// <para><b>Run architecture tests against Debug builds.</b> In a Release build the C# compiler
    /// emits async state machines as structs, and ArchUnitNET drops those compiler-generated value
    /// types — every dependency that occurs only inside an <c>async</c> method body becomes invisible
    /// to dependency rules. In a Debug build the state machine is a class whose dependencies
    /// ArchUnitNET attributes to the declaring type. This method therefore refuses JIT-optimized
    /// assemblies; use <see cref="Load(DcaLayout, bool, Assembly[])"/> with
    /// <c>allowOptimizedAssemblies: true</c> to accept the blind spot knowingly.</para>
    /// </remarks>
    public static DcaArchitecture Load(DcaLayout layout, params Assembly[] assemblies) =>
        Load(layout, allowOptimizedAssemblies: false, assemblies);

    /// <summary>
    /// Like <see cref="Load(DcaLayout, Assembly[])"/>; with <paramref name="allowOptimizedAssemblies"/>
    /// set, Release-built assemblies are accepted although dependencies inside <c>async</c> method
    /// bodies are then not analysed.
    /// </summary>
    public static DcaArchitecture Load(DcaLayout layout, bool allowOptimizedAssemblies, params Assembly[] assemblies)
    {
        if (layout is null)
        {
            throw new ArgumentNullException(nameof(layout));
        }

        if (assemblies is null || assemblies.Length == 0)
        {
            throw new ArgumentException("at least one assembly is required", nameof(assemblies));
        }

        if (!allowOptimizedAssemblies)
        {
            var optimized = assemblies.Where(IsJitOptimized).Select(a => a.GetName().Name).ToList();
            if (optimized.Count > 0)
            {
                throw new InvalidOperationException(
                    "Architecture tests must run against Debug builds: " + string.Join(", ", optimized) +
                    " is JIT-optimized (Release). ArchUnitNET drops the struct-based async state machines of " +
                    "optimized builds, so dependencies inside async method bodies would not be analysed. " +
                    "Build the assemblies under test in Debug configuration, or pass allowOptimizedAssemblies: true " +
                    "to accept that blind spot.");
            }
        }

        // The building blocks are always part of the model: ArchUnitNET resolves `typeof(IAggregateRoot)`
        // and friends against the loaded architecture and throws when a marker is not referenced anywhere
        // (a project without domain services would fail every rule that mentions IDomainService).
        var loader = new ArchLoader().LoadAssembly(typeof(BoundedContextAttribute).Assembly);
        foreach (var assembly in assemblies)
        {
            loader.LoadNamespacesWithinAssembly(assembly, layout.RootNamespace);
        }

        return new DcaArchitecture(layout, loader.Build(), assemblies);
    }

    /// <summary>Wraps an already built ArchUnitNET architecture — for tests and custom loaders.</summary>
    public static DcaArchitecture Of(DcaLayout layout, Architecture architecture, params Assembly[] assemblies) =>
        new(layout, architecture, assemblies ?? Array.Empty<Assembly>());

    public DcaLayout Layout { get; }

    /// <summary>The ArchUnitNET model of the imported types.</summary>
    public Architecture Architecture { get; }

    /// <summary>Imported types below the root namespace (ArchUnitNET model, referenced types excluded).</summary>
    public IEnumerable<IType> Types =>
        Architecture.Types.Where(t => t.Namespace is not null && DcaLayout.IsBelow(t.Namespace.FullName, Layout.RootNamespace));

    /// <summary>Imported classes below the root namespace.</summary>
    public IEnumerable<Class> Classes => Types.OfType<Class>();

    /// <summary>Imported interfaces below the root namespace.</summary>
    public IEnumerable<Interface> Interfaces => Types.OfType<Interface>();

    // ---------------------------------------------------------------------------------------------
    // Bounded-context discovery
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// All namespaces directly below the root namespace that carry a <c>[BoundedContext]</c> marker class,
    /// keyed by namespace, in encounter order.
    /// </summary>
    public IReadOnlyDictionary<string, BoundedContextAttribute> BoundedContexts
    {
        get
        {
            if (_boundedContexts is null)
            {
                var found = new Dictionary<string, BoundedContextAttribute>();
                var order = new List<string>();
                foreach (var root in RootNamespaces())
                {
                    var attribute = NamespaceAttribute<BoundedContextAttribute>(root);
                    if (attribute is not null && !found.ContainsKey(root))
                    {
                        found[root] = attribute;
                        order.Add(root);
                    }
                }

                _boundedContexts = new OrderedReadOnlyDictionary<BoundedContextAttribute>(order, found);
            }

            return _boundedContexts;
        }
    }

    /// <summary>Namespaces of all bounded contexts (plain names).</summary>
    public IReadOnlyList<string> BoundedContextNamespaces => BoundedContexts.Keys.ToList();

    /// <summary>Bounded-context patterns, e.g. the regex for <c>Acme.Shop.Cart</c> and below.</summary>
    public string[] BoundedContextPatterns() => BoundedContexts.Keys.Select(DcaLayout.Below).ToArray();

    /// <summary>Bounded-context patterns without the given context namespace.</summary>
    public string[] BoundedContextPatternsExcluding(string contextNamespace) =>
        BoundedContexts.Keys.Where(p => p != contextNamespace).Select(DcaLayout.Below).ToArray();

    /// <summary>The namespace directly below the root namespace whose marker class carries <c>[SharedKernel]</c>.</summary>
    public string? SharedKernelNamespace
    {
        get
        {
            if (!_sharedKernelResolved)
            {
                _sharedKernelNamespace = RootNamespaces().FirstOrDefault(r => NamespaceAttribute<SharedKernelAttribute>(r) is not null);
                _sharedKernelResolved = true;
            }

            return _sharedKernelNamespace;
        }
    }

    /// <summary>
    /// The direct child namespace of the root namespace that a full namespace belongs to, e.g.
    /// <c>Acme.Shop.Cart.Domain.Model → Acme.Shop.Cart</c>; <c>null</c> for namespaces outside the root.
    /// </summary>
    public string? RootContextNamespace(string fullNamespace)
    {
        var prefix = Layout.RootNamespace + ".";
        if (fullNamespace is null || !fullNamespace.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var remainder = fullNamespace.Substring(prefix.Length);
        var dot = remainder.IndexOf('.');
        if (dot > 0)
        {
            return Layout.RootNamespace + "." + remainder.Substring(0, dot);
        }

        return remainder.Length == 0 ? null : Layout.RootNamespace + "." + remainder;
    }

    /// <summary>Last segment of a context namespace: <c>Acme.Shop.Cart → Cart</c>.</summary>
    public static string SimpleContextName(string contextNamespace) =>
        contextNamespace.Substring(contextNamespace.LastIndexOf('.') + 1);

    // ---------------------------------------------------------------------------------------------
    // Namespace-level attributes (marker classes)
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// The single attribute of type <typeparamref name="T"/> declared on a marker class residing exactly
    /// in <paramref name="ns"/>, or <c>null</c>.
    /// </summary>
    public T? NamespaceAttribute<T>(string ns)
        where T : Attribute =>
        NamespaceAttributes<T>(ns).FirstOrDefault();

    /// <summary>
    /// All attributes of type <typeparamref name="T"/> declared on classes residing exactly in
    /// <paramref name="ns"/> (repeatable attributes such as <c>[Upstream]</c>).
    /// </summary>
    public IReadOnlyList<T> NamespaceAttributes<T>(string ns)
        where T : Attribute =>
        RootTypes()
            .Where(t => t.Namespace == ns)
            .SelectMany(t => t.GetCustomAttributes<T>(inherit: false))
            .ToList();

    /// <summary>The runtime <see cref="Type"/>s of the loaded assemblies below the root namespace.</summary>
    public IReadOnlyList<Type> RuntimeTypes() => RootTypes();

    /// <summary>The runtime <see cref="Type"/> behind an ArchUnitNET type, or <c>null</c> when it is not in the loaded assemblies.</summary>
    public Type? RuntimeType(IType type) =>
        RootTypes().FirstOrDefault(t => t.FullName == type.FullName.Replace('+', '+')) ??
        _assemblies.Select(a => a.GetType(type.FullName, throwOnError: false)).FirstOrDefault(t => t is not null);

    private IReadOnlyList<Type> RootTypes()
    {
        if (_rootTypes is null)
        {
            _rootTypes = _assemblies
                .SelectMany(SafeGetTypes)
                .Where(t => t.Namespace is not null && DcaLayout.IsBelow(t.Namespace, Layout.RootNamespace))
                .ToList();
        }

        return _rootTypes;
    }

    /// <summary>Whether the assembly was compiled with optimizations (Release) according to its <see cref="System.Diagnostics.DebuggableAttribute"/>.</summary>
    public static bool IsJitOptimized(Assembly assembly)
    {
        var debuggable = assembly.GetCustomAttribute<System.Diagnostics.DebuggableAttribute>();
        return debuggable is null || !debuggable.IsJITOptimizerDisabled;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t is not null)!;
        }
    }

    /// <summary>Distinct direct child namespaces of the root namespace, in encounter order.</summary>
    private IEnumerable<string> RootNamespaces()
    {
        var seen = new HashSet<string>();
        foreach (var type in RootTypes())
        {
            var root = RootContextNamespace(type.Namespace!);
            if (root is not null && seen.Add(root))
            {
                yield return root;
            }
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Per-context patterns
    // ---------------------------------------------------------------------------------------------

    public string[] ContextDomainPatterns() => BoundedContexts.Keys.Select(Layout.DomainPatternOf).ToArray();

    public string[] ContextDomainModelPatterns() => BoundedContexts.Keys.Select(Layout.DomainModelPatternOf).ToArray();

    public string[] ContextApplicationPatterns() => BoundedContexts.Keys.Select(Layout.ApplicationPatternOf).ToArray();

    public string[] ContextAdapterPatterns() => BoundedContexts.Keys.Select(Layout.AdapterPatternOf).ToArray();

    /// <summary>Incoming-adapter patterns of all contexts plus the shared kernel's, if it has one.</summary>
    public string[] AllIncomingAdapterPatterns() => WithSharedKernel(Layout.IncomingAdapterPatternOf);

    /// <summary>Outgoing-adapter patterns of all contexts plus the shared kernel's, if it has one.</summary>
    public string[] AllOutgoingAdapterPatterns() => WithSharedKernel(Layout.OutgoingAdapterPatternOf);

    /// <summary>Domain patterns of all contexts plus the shared kernel domain.</summary>
    public string[] AllDomainPatternsWithSharedKernel() =>
        ContextDomainPatterns().Append(Layout.SharedKernelDomainPattern).ToArray();

    /// <summary>Domain-model patterns of all contexts plus the shared kernel domain.</summary>
    public string[] AllDomainModelPatternsWithSharedKernel() =>
        ContextDomainModelPatterns().Append(Layout.SharedKernelDomainPattern).ToArray();

    /// <summary>Whether a type resides in the global infrastructure namespace (or below).</summary>
    public bool IsInfrastructureImplementation(IType type) =>
        type.Namespace is not null && DcaLayout.IsBelow(type.Namespace.FullName, Layout.InfrastructureNamespace);

    private string[] WithSharedKernel(Func<string, string> pattern)
    {
        var patterns = BoundedContexts.Keys.Select(pattern).ToList();
        if (SharedKernelNamespace is not null)
        {
            patterns.Add(pattern(SharedKernelNamespace));
        }

        return patterns.ToArray();
    }

    private sealed class OrderedReadOnlyDictionary<TValue> : IReadOnlyDictionary<string, TValue>
    {
        private readonly List<string> _order;
        private readonly Dictionary<string, TValue> _values;

        public OrderedReadOnlyDictionary(List<string> order, Dictionary<string, TValue> values)
        {
            _order = order;
            _values = values;
        }

        public TValue this[string key] => _values[key];

        public IEnumerable<string> Keys => _order;

        public IEnumerable<TValue> Values => _order.Select(k => _values[k]);

        public int Count => _order.Count;

        public bool ContainsKey(string key) => _values.ContainsKey(key);

        public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator() =>
            _order.Select(k => new KeyValuePair<string, TValue>(k, _values[k])).GetEnumerator();

        public bool TryGetValue(string key, out TValue value) => _values.TryGetValue(key, out value!);

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
