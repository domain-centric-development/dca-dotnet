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
    private IReadOnlyList<string>? _moduleRoots;
    private readonly Dictionary<string, string?> _rootContextCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> _contextRootCache = new(StringComparer.Ordinal);

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
    // Bounded-context discovery — by declaration, at any depth
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// All namespaces at or below the root namespace that carry a <c>[BoundedContext]</c> marker class,
    /// keyed by namespace, in encounter order. A context may sit at any depth (<c>Acme.Shop.Cart</c>,
    /// <c>Acme.Shop.Sales.Order</c>, or the root namespace itself); the declaration is the attribute, not
    /// the position in the namespace tree.
    /// </summary>
    public IReadOnlyDictionary<string, BoundedContextAttribute> BoundedContexts
    {
        get
        {
            if (_boundedContexts is null)
            {
                var found = new Dictionary<string, BoundedContextAttribute>();
                var order = new List<string>();
                foreach (var root in ContextRoots())
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

    /// <summary>The namespace, at whatever depth below the root, whose marker class carries <c>[SharedKernel]</c>.</summary>
    public string? SharedKernelNamespace
    {
        get
        {
            if (!_sharedKernelResolved)
            {
                _sharedKernelNamespace = ContextRoots().FirstOrDefault(r => NamespaceAttribute<SharedKernelAttribute>(r) is not null);
                _sharedKernelResolved = true;
            }

            return _sharedKernelNamespace;
        }
    }

    /// <summary>
    /// The root namespace of the bounded context (or shared kernel) a full namespace belongs to — the
    /// nearest ancestor at or below the root namespace whose marker class carries <c>[BoundedContext]</c>
    /// or <c>[SharedKernel]</c>: <c>Acme.Shop.Cart.Domain.Model → Acme.Shop.Cart</c> when <c>Cart</c> is
    /// declared, <c>Acme.Shop.Sales.Order.Domain.Model → Acme.Shop.Sales.Order</c> when <c>Sales.Order</c> is.
    /// </summary>
    /// <remarks>
    /// Falls back to the direct child namespace of the root when no ancestor is declared, so that a
    /// project which has not declared its contexts yet still groups types the way it used to. Such a
    /// namespace is not a discovered context; whether it owns layers — and is therefore governed — is
    /// decided structurally by <see cref="ModuleRoots"/>, not by this fallback.
    /// </remarks>
    /// <returns>the context root namespace, or <c>null</c> for namespaces outside the root namespace</returns>
    public string? RootContextNamespace(string fullNamespace)
    {
        if (fullNamespace is null)
        {
            return null;
        }

        var root = Layout.RootNamespace;
        if (!DcaLayout.IsBelow(fullNamespace, root))
        {
            return null;
        }

        if (_rootContextCache.TryGetValue(fullNamespace, out var cached))
        {
            return cached;
        }

        string? result = null;
        for (var candidate = fullNamespace; candidate is not null; candidate = ParentNamespace(candidate, root))
        {
            if (IsContextRoot(candidate))
            {
                result = candidate;
                break;
            }
        }

        result ??= FirstSegmentBelowRoot(fullNamespace, root);
        _rootContextCache[fullNamespace] = result;
        return result;
    }

    /// <summary>
    /// The identifier of a context: its namespace relative to the root namespace —
    /// <c>Acme.Shop.Cart → Cart</c>, <c>Acme.Shop.Sales.Order → Sales.Order</c>. For a single-context
    /// application whose root namespace is the context, the root's last segment. Unambiguous for grouped
    /// contexts, where the last segment alone would not be; identical to the last segment for a context
    /// that is a direct child of the root, so existing <c>[Upstream("Cart")]</c> declarations keep working.
    /// </summary>
    public string ContextName(string contextNamespace)
    {
        var root = Layout.RootNamespace;
        if (contextNamespace == root)
        {
            return LastSegment(root);
        }

        return contextNamespace.StartsWith(root + ".", StringComparison.Ordinal)
            ? contextNamespace.Substring(root.Length + 1)
            : LastSegment(contextNamespace);
    }

    /// <summary>Last segment of a namespace: <c>Acme.Shop.Cart → Cart</c>.</summary>
    /// <remarks>A context identifier is its name relative to the root namespace — use
    /// <see cref="ContextName"/>, which is unambiguous for grouped contexts. This method stays for the
    /// case where only a last segment is wanted.</remarks>
    [Obsolete("Use ContextName(string): a context is identified by its namespace relative to the root namespace.")]
    public static string SimpleContextName(string contextNamespace) => LastSegment(contextNamespace);

    private static string LastSegment(string ns) => ns.Substring(ns.LastIndexOf('.') + 1);

    private bool IsContextRoot(string ns)
    {
        if (!_contextRootCache.TryGetValue(ns, out var isRoot))
        {
            isRoot = NamespaceAttribute<BoundedContextAttribute>(ns) is not null
                || NamespaceAttribute<SharedKernelAttribute>(ns) is not null;
            _contextRootCache[ns] = isRoot;
        }

        return isRoot;
    }

    /// <summary>The parent of a namespace, or <c>null</c> at (or above) the root namespace.</summary>
    private static string? ParentNamespace(string ns, string root)
    {
        if (ns == root)
        {
            return null;
        }

        var dot = ns.LastIndexOf('.');
        return dot < 0 ? null : ns.Substring(0, dot);
    }

    private static string? FirstSegmentBelowRoot(string fullNamespace, string root)
    {
        if (fullNamespace == root)
        {
            return null;
        }

        var remainder = fullNamespace.Substring(root.Length + 1);
        var dot = remainder.IndexOf('.');
        return root + "." + (dot > 0 ? remainder.Substring(0, dot) : remainder);
    }

    /// <summary>Distinct context roots of all loaded types, in encounter order.</summary>
    private IEnumerable<string> ContextRoots()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
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
    // Module discovery — structural, unlike context discovery
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Every namespace that owns a DCA layer, in encounter order — the roots the layer rules apply to.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this is not the same as <see cref="BoundedContexts"/>.</b> Being a bounded context is
    /// a strategic statement: it is declared with <c>[BoundedContext]</c> and it decides the context map
    /// and the upstream relationships. Owning a <c>Domain</c>/<c>Application</c>/<c>Adapter</c> layer is
    /// a structural fact, and the layer rules — the domain knows no infrastructure, transactions are an
    /// application concern, a <c>Command</c> lives in <c>Application</c> — apply to it either way. A
    /// module that is deliberately <em>not</em> a bounded context still follows DCA layering and must
    /// still be governed.</para>
    /// <para>A root is the <b>shortest</b> namespace prefix at or below the root namespace whose remainder
    /// starts with a layer segment. Shortest wins so that an adapter's own <c>Domain</c> namespace — an
    /// outgoing adapter mapping to a foreign model — stays inside its module instead of becoming a root
    /// of its own: for <c>Root.Cart.Adapter.Outgoing.Domain.Foo</c> the root is <c>Root.Cart</c>.</para>
    /// <para>Because the test is structural, a module is found at any depth and without any attribute —
    /// which is what keeps a grouped or nested layout governed. The isolation rules select over module
    /// roots as well (<see cref="IsolatedModuleRoots"/>), so a module that declares nothing can neither
    /// reach into a neighbour's internals nor have its own internals reached into. Declaring a module a
    /// bounded context decides its place on the context map, nothing more.</para>
    /// </remarks>
    public IReadOnlyList<string> ModuleRoots()
    {
        if (_moduleRoots is null)
        {
            var layers = LayerSegments();
            var roots = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var type in Types)
            {
                var root = ModuleRootOf(type.Namespace!.FullName, Layout.RootNamespace, layers);
                if (root is not null && seen.Add(root))
                {
                    roots.Add(root);
                }
            }

            _moduleRoots = roots;
        }

        return _moduleRoots;
    }

    /// <summary>The layer segments of this layout: <c>Domain</c>, <c>Application</c>, <c>Adapter</c>.</summary>
    public ISet<string> LayerSegments() =>
        new HashSet<string>(new[] { Layout.DomainSegment, Layout.ApplicationSegment, Layout.AdapterSegment }, StringComparer.Ordinal);

    /// <summary>
    /// The module root of a namespace, or <c>null</c> when the namespace carries no layer segment — the
    /// global infrastructure namespace, for instance, or a plain support namespace.
    /// </summary>
    public string? ModuleRootOf(string ns) => ModuleRootOf(ns, Layout.RootNamespace, LayerSegments());

    private static string? ModuleRootOf(string? ns, string root, ISet<string> layers)
    {
        if (ns is null || !ns.StartsWith(root + ".", StringComparison.Ordinal))
        {
            return null;
        }

        var current = root;
        foreach (var segment in ns.Substring(root.Length + 1).Split('.'))
        {
            if (layers.Contains(segment))
            {
                return current;
            }

            current = current + "." + segment;
        }

        return null;
    }

    /// <summary>
    /// The module roots the isolation rules govern: every module root except the shared kernel. The
    /// shared kernel is a module root too (it may own <c>Domain</c>, <c>Application</c> and <c>Adapter</c>
    /// namespaces), but everyone may depend on it, and what <em>it</em> may depend on is <c>DCA-STR-002</c>'s
    /// business.
    /// </summary>
    public IReadOnlyList<string> IsolatedModuleRoots()
    {
        var sharedKernel = SharedKernelNamespace;
        return ModuleRoots().Where(root => root != sharedKernel).ToList();
    }

    /// <summary>
    /// Patterns of every isolated module root except the given one — <c>root</c> and below, each. Built
    /// from <see cref="IsolatedModuleRoots"/>, so an undeclared module is a forbidden target like any
    /// other, not only a governed source.
    /// </summary>
    public string[] ModuleRootPatternsExcluding(string moduleRoot) =>
        IsolatedModuleRoots().Where(root => root != moduleRoot).Select(DcaLayout.Below).ToArray();

    /// <summary>
    /// The published namespaces of every isolated module root except the given one: <c>root.Api</c>
    /// (synchronous, in-process) and <c>root.Events</c> (asynchronous) and below, with the segment names
    /// taken from <see cref="DcaLayout.PublishedSegments"/>. DCA's in-process contract convention —
    /// namespace names, a convention of the architecture and not of any framework — and the only part
    /// of a foreign module an adapter may depend on.
    /// </summary>
    public string[] PublishedPatternsExcluding(string moduleRoot) =>
        IsolatedModuleRoots()
            .Where(root => root != moduleRoot)
            .SelectMany(root => Layout.PublishedSegments.Select(segment => DcaLayout.Below(root + "." + segment)))
            .ToArray();

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

    // ---------------------------------------------------------------------------------------------
    // Patterns: Context* over declared bounded contexts, All* over module roots
    // ---------------------------------------------------------------------------------------------

    public string[] ContextDomainPatterns() => BoundedContexts.Keys.Select(Layout.DomainPatternOf).ToArray();

    public string[] ContextDomainModelPatterns() => BoundedContexts.Keys.Select(Layout.DomainModelPatternOf).ToArray();

    public string[] ContextApplicationPatterns() => BoundedContexts.Keys.Select(Layout.ApplicationPatternOf).ToArray();

    public string[] ContextAdapterPatterns() => BoundedContexts.Keys.Select(Layout.AdapterPatternOf).ToArray();

    /// <summary>Domain patterns of every module root — the layer rules' selection, at any depth.</summary>
    public string[] AllDomainPatterns() => ModuleRoots().Select(Layout.DomainPatternOf).ToArray();

    /// <summary>Domain-model patterns of every module root.</summary>
    public string[] AllDomainModelPatterns() => ModuleRoots().Select(Layout.DomainModelPatternOf).ToArray();

    /// <summary>Application patterns of every module root.</summary>
    public string[] AllApplicationPatterns() => ModuleRoots().Select(Layout.ApplicationPatternOf).ToArray();

    /// <summary>Shared-output-port patterns (<c>Application.Shared</c>) of every module root.</summary>
    public string[] AllSharedOutputPortPatterns() => ModuleRoots().Select(Layout.SharedOutputPortPatternOf).ToArray();

    /// <summary>Adapter patterns of every module root.</summary>
    public string[] AllAdapterPatterns() => ModuleRoots().Select(Layout.AdapterPatternOf).ToArray();

    /// <summary>Incoming-adapter patterns of every module root (contexts, shared kernel and undeclared modules alike).</summary>
    public string[] AllIncomingAdapterPatterns() => ModuleRoots().Select(Layout.IncomingAdapterPatternOf).ToArray();

    /// <summary>Outgoing-adapter patterns of every module root.</summary>
    public string[] AllOutgoingAdapterPatterns() => ModuleRoots().Select(Layout.OutgoingAdapterPatternOf).ToArray();

    /// <summary>Domain patterns of every module root — the shared kernel is a module root when it owns a domain layer.</summary>
    [Obsolete("Use AllDomainPatterns(): module roots include the shared kernel.")]
    public string[] AllDomainPatternsWithSharedKernel() => AllDomainPatterns();

    /// <summary>Domain-model patterns of every module root.</summary>
    [Obsolete("Use AllDomainModelPatterns(): module roots include the shared kernel.")]
    public string[] AllDomainModelPatternsWithSharedKernel() => AllDomainModelPatterns();

    /// <summary>
    /// The infrastructure namespaces of this architecture: the global one (<c>Root.Infrastructure</c>) and
    /// every isolated module's own (<c>Root.Cart.Infrastructure</c>), each without pattern. A module's
    /// infrastructure is not a layer — it does not make the module a root — but it is an implementation
    /// detail like the global one, and the rules that keep implementation details out of the inner layers
    /// treat both alike. The shared kernel's <c>Infrastructure</c> namespace is deliberately not listed:
    /// the shared kernel is the one namespace everyone may depend on, and what it keeps there is shared
    /// support, not a detail of one module.
    /// </summary>
    public IReadOnlyList<string> InfrastructureNamespaces()
    {
        var namespaces = new List<string> { Layout.InfrastructureNamespace };
        foreach (var root in IsolatedModuleRoots())
        {
            var module = root + "." + Layout.InfrastructureSegment;
            if (!namespaces.Contains(module))
            {
                namespaces.Add(module);
            }
        }

        return namespaces;
    }

    /// <summary><see cref="InfrastructureNamespaces"/> as patterns, each namespace and below.</summary>
    public string[] AllInfrastructurePatterns() => InfrastructureNamespaces().Select(DcaLayout.Below).ToArray();

    /// <summary>
    /// Whether a type resides in an infrastructure namespace — the namespace itself or below, with an exact
    /// segment boundary: <c>Root.Infrastructure.Wiring</c> counts, <c>Root.InfrastructureX.Other</c> does not.
    /// </summary>
    public bool IsInfrastructureImplementation(IType type) =>
        type.Namespace is not null
        && InfrastructureNamespaces().Any(ns => DcaLayout.IsBelow(type.Namespace.FullName, ns));

    /// <summary>
    /// Every namespace at or below the root namespace that a loaded type lives in, together with all its
    /// ancestors down to the root namespace, in encounter order — the set of namespaces that may carry a
    /// marker class with context-map declarations.
    /// </summary>
    public IReadOnlyList<string> NamespacesBelowRoot()
    {
        var root = Layout.RootNamespace;
        var namespaces = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in RootTypes())
        {
            for (var candidate = type.Namespace; candidate is not null; candidate = ParentNamespace(candidate, root))
            {
                if (seen.Add(candidate))
                {
                    namespaces.Add(candidate);
                }
            }
        }

        return namespaces;
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
