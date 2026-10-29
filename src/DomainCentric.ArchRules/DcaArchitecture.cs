using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;
using Assembly = System.Reflection.Assembly;
using Attribute = System.Attribute;
using Type = System.Type;

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
/// <see cref="Load(DcaLayout, Assembly[])"/> must be the loaded runtime assemblies. The context and
/// shared-kernel attributes are matched by the full type name their marker role names
/// (<see cref="DcaMarkers.BoundedContext"/>, <see cref="DcaMarkers.SharedKernel"/>), so a project's own
/// attribute serves like the library's.</para>
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
    private readonly Dictionary<string, Placement?> _placementCache = new(StringComparer.Ordinal);
    private IReadOnlyList<string>? _infrastructureNamespaces;
    private IReadOnlyList<NamespaceDeclaration>? _namespaceDeclarations;
    private readonly Dictionary<string, (DcaLayout.Layer Layer, string LayerNamespace)?> _assemblyDeclarations = new(StringComparer.Ordinal);

    /// <summary>
    /// Where a type sits: its layer, the namespace that placed it there — the namespace an assembly attribute
    /// names, the type's own namespace for an attribute on the type, the namespace the assembly's types share
    /// for an attribute on the assembly, <c>&lt;root&gt;.&lt;segment&gt;</c> for the path — and how.
    /// </summary>
    public sealed record Placement(DcaLayout.Layer Layer, string LayerNamespace, PlacementSource Source);

    /// <summary>How a <see cref="Placement"/> was found, in precedence order.</summary>
    public enum PlacementSource
    {
        /// <summary>A layer attribute on the type itself.</summary>
        Type,

        /// <summary>An assembly attribute whose first constructor argument names the type's namespace or one above it — the nearest wins.</summary>
        Namespace,

        /// <summary>A layer attribute on the type's assembly, naming no namespace.</summary>
        Assembly,

        /// <summary>The namespace path below a module root.</summary>
        Path,
    }

    private sealed record NamespaceDeclaration(string Namespace, DcaLayout.Layer Layer, string AssemblyName);

    private DcaArchitecture(DcaLayout layout, Architecture architecture, IReadOnlyList<Assembly> assemblies)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Architecture = WithoutExcludedNamespaces(layout, architecture ?? throw new ArgumentNullException(nameof(architecture)));
        _assemblies = assemblies;
    }

    /// <summary>
    /// The architecture without the types of <see cref="DcaLayout.ExcludedNamespaces"/>. They stay known as
    /// referenced types, so a dependency on them is still a dependency; they are only no longer checked.
    /// </summary>
    private static Architecture WithoutExcludedNamespaces(DcaLayout layout, Architecture architecture)
    {
        if (layout.ExcludedNamespaces.Count == 0)
        {
            return architecture;
        }

        bool Excluded(IType type) => layout.IsExcluded(type.Namespace?.FullName);
        var types = architecture.Types.Where(t => !Excluded(t)).ToList();
        var namespaces = architecture.Namespaces
            .Where(ns => !layout.IsExcluded(ns.FullName))
            .Select(ns => new Namespace(ns.FullName, ns.Types.Where(t => !Excluded(t)).ToList()))
            .ToList();
        return new Architecture(
            architecture.Assemblies,
            namespaces,
            types,
            architecture.GenericParameters,
            architecture.ReferencedTypes.Concat(architecture.Types.Where(Excluded)).ToList(),
            true);
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

        // The building blocks are always part of the model: ArchUnitNET resolves `arch.Layout.Markers.AggregateRoot`
        // and friends against the loaded architecture and throws when a marker is not referenced anywhere
        // (a project without domain services would fail every rule that mentions IDomainService).
        var loader = new ArchLoader().LoadAssembly(typeof(BoundedContextAttribute).Assembly);
        foreach (var assembly in assemblies)
        {
            loader.LoadNamespacesWithinAssembly(assembly, layout.RootNamespace);
            // A consumer type declared in the reserved building-blocks port namespace lies outside the root
            // namespace; DCA-LAY-005 can only report it when it is imported as well.
            loader.LoadNamespacesWithinAssembly(assembly, DcaLayout.BuildingBlocksPortsOutNamespace);
        }

        RequireImportedTypes(layout, assemblies);

        return new DcaArchitecture(layout, loader.Build(), assemblies);
    }

    /// <summary>
    /// Refuses a run whose assemblies hold no type the layout describes: every rule would then pass
    /// without having looked at anything. Reflection over the passed assemblies, not the built model —
    /// the always-loaded building blocks would otherwise make the model look populated.
    /// </summary>
    private static void RequireImportedTypes(DcaLayout layout, IReadOnlyList<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            foreach (var type in Exported(assembly))
            {
                if (type.Namespace is not null
                    && (DcaLayout.IsBelow(type.Namespace, layout.RootNamespace)
                        || DcaLayout.IsBelow(type.Namespace, DcaLayout.BuildingBlocksPortsOutNamespace)))
                {
                    return;
                }
            }
        }

        throw new InvalidOperationException(
            $"No type was imported below the root namespace '{layout.RootNamespace}', so every rule would " +
            "pass without having looked at anything. Check that the root namespace is spelled as the " +
            "production code declares it, and that every production project holding code below it is " +
            "referenced by the test project and passed to Load. Assemblies passed: " +
            string.Join(", ", assemblies.Select(a => a.GetName().Name)) + ".");
    }

    private static IEnumerable<Type> Exported(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException partiallyLoaded)
        {
            return partiallyLoaded.Types.Where(t => t is not null)!;
        }
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

    /// <summary>The imported type of that full name below the root namespace, or <c>null</c> — to place a reflected type.</summary>
    public IType? TypeNamed(string? fullName)
    {
        _typesByName ??= Types.GroupBy(t => t.FullName, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        return fullName is not null && _typesByName.TryGetValue(fullName, out var type) ? type : null;
    }

    private Dictionary<string, IType>? _typesByName;

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

    /// <summary>
    /// The namespace, at whatever depth below the root, whose marker class carries <c>[SharedKernel]</c> — or
    /// the attribute the <see cref="DcaMarkers.SharedKernel"/> role names. Where no namespace carries it, the
    /// conventional namespace <c>Root.&lt;SharedKernelSegment&gt;</c> is the shared kernel when it holds types: a
    /// project that has not declared its kernel yet still gets the kernel's exemption from module isolation.
    /// </summary>
    public string? SharedKernelNamespace
    {
        get
        {
            if (!_sharedKernelResolved)
            {
                _sharedKernelNamespace = ContextRoots().FirstOrDefault(r => HasNamespaceAttribute(r, Layout.Markers.SharedKernel));
                if (_sharedKernelNamespace is null
                    && RootTypes().Any(t => DcaLayout.IsBelow(t.Namespace!, Layout.SharedKernelNamespace)))
                {
                    _sharedKernelNamespace = Layout.SharedKernelNamespace;
                }

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
            isRoot = HasNamespaceAttribute(ns, Layout.Markers.BoundedContext)
                || HasNamespaceAttribute(ns, Layout.Markers.SharedKernel);
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

            if (Layout.HasLayerAttributes)
            {
                foreach (var type in Types)
                {
                    var placement = AttributePlacement(type);
                    if (placement is not null && placement.Layer != DcaLayout.Layer.Infrastructure)
                    {
                        var root = AttributedModuleRoot(placement);
                        if (root is not null && seen.Add(root))
                        {
                            roots.Add(root);
                        }
                    }
                }
            }

            _moduleRoots = roots;
        }

        return _moduleRoots;
    }

    /// <summary>The layer segments of this layout: <c>Domain</c>, <c>Application</c>, <c>Adapter</c>.</summary>
    public ISet<string> LayerSegments()
    {
        var segments = new HashSet<string>(StringComparer.Ordinal);
        if (Layout.LayerHasNamespace(DcaLayout.Layer.Domain))
        {
            segments.Add(Layout.DomainSegment);
        }

        if (Layout.LayerHasNamespace(DcaLayout.Layer.Application))
        {
            segments.Add(Layout.ApplicationSegment);
        }

        if (Layout.LayerHasNamespace(DcaLayout.Layer.IncomingAdapter) || Layout.LayerHasNamespace(DcaLayout.Layer.OutgoingAdapter))
        {
            segments.Add(Layout.AdapterSegment);
        }

        return segments;
    }

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
    /// A finding about the layout itself, or <c>null</c>: the root namespace resolves as a module root next to
    /// other module roots. That happens when a namespace directly below the root namespace is named like a
    /// layer — a host in <c>Root.Application</c>, say — and it makes every module a neighbour of every other,
    /// because each one lies inside the root namespace. <see cref="DcaLayout.WithExcludedNamespaces"/> leaves
    /// such a module out.
    /// </summary>
    public string? RootModuleRootNotice()
    {
        var root = Layout.RootNamespace;
        var roots = ModuleRoots();
        if (!roots.Contains(root) || roots.Count < 2)
        {
            return null;
        }

        var layerNamespaces = Types
            .Select(t => t.Namespace!.FullName)
            .Where(ns => ModuleRootOf(ns) == root)
            .Select(ns => root + "." + ns.Substring(root.Length + 1).Split('.')[0])
            .Distinct(StringComparer.Ordinal)
            .OrderBy(ns => ns, StringComparer.Ordinal)
            .ToList();
        return $"the root namespace {root} is a module root next to {roots.Count - 1} others, because of types in "
            + string.Join(", ", layerNamespaces)
            + " - every module then lies inside it. If that is a module that only assembles the application,"
            + " leave it out with DcaLayout.WithExcludedNamespaces(..)";
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
    // Layer placement — attribute on the type, then on its namespace or assembly, then the path
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// The layer a type sits in, or <c>null</c>. A layer attribute the layout names on the type wins; then an
    /// assembly attribute of that type whose first constructor argument names the type's namespace or the
    /// nearest namespace above it (a declaration holds for its whole subtree); then the attribute on the
    /// type's own assembly; then the path below its module root. Without layer attributes in the layout this
    /// is the path alone, exactly the namespaces the <c>All…Patterns</c> name.
    /// </summary>
    public Placement? PlacementOf(IType type)
    {
        if (!_placementCache.TryGetValue(type.FullName, out var placement))
        {
            placement = AttributePlacement(type) ?? PathPlacement(type.Namespace?.FullName);
            _placementCache[type.FullName] = placement;
        }

        return placement;
    }

    /// <summary>
    /// The placement of a reflected type — the same answer as <see cref="PlacementOf(IType)"/>, for a rule that walks
    /// runtime types (a result's members, an enum an adapter formats).
    /// </summary>
    public Placement? PlacementOf(Type type)
    {
        if (type.FullName is null)
        {
            return null;
        }

        if (TypeNamed(type.FullName) is { } imported)
        {
            return PlacementOf(imported);
        }

        if (!_placementCache.TryGetValue(type.FullName, out var placement))
        {
            IEnumerable<string?> attributes;
            try
            {
                attributes = type.GetCustomAttributesData().Select(a => a.AttributeType.FullName).ToList();
            }
            catch (Exception e) when (e is TypeLoadException or FileNotFoundException or FileLoadException)
            {
                attributes = Array.Empty<string?>();
            }

            placement = AttributePlacement(attributes, type.Namespace ?? "", type.Assembly.FullName) ?? PathPlacement(type.Namespace);
            _placementCache[type.FullName] = placement;
        }

        return placement;
    }

    /// <summary>The placement by attribute alone — needs no module root, so module discovery can ask it.</summary>
    private Placement? AttributePlacement(IType type) =>
        Layout.HasLayerAttributes
            ? AttributePlacement(type.Attributes.Select(a => (string?)a.FullName), type.Namespace?.FullName ?? "", type.Assembly?.FullName)
            : null;

    private Placement? AttributePlacement(IEnumerable<string?> attributeNames, string ns, string? assemblyName)
    {
        if (!Layout.HasLayerAttributes)
        {
            return null;
        }

        foreach (var attribute in attributeNames)
        {
            var layer = LayerAttributedBy(attribute);
            if (layer is not null)
            {
                return new Placement(layer.Value, ns, PlacementSource.Type);
            }
        }

        var declared = NamespaceDeclarationOf(ns);
        if (declared is not null)
        {
            return declared;
        }

        if (assemblyName is not null && AssemblyDeclaration(assemblyName) is { } assembly)
        {
            return new Placement(assembly.Layer, assembly.LayerNamespace, PlacementSource.Assembly);
        }

        return null;
    }

    /// <summary>The nearest namespace at or above <paramref name="ns"/> that an assembly attribute declares a layer for.</summary>
    private Placement? NamespaceDeclarationOf(string ns)
    {
        NamespaceDeclaration? nearest = null;
        foreach (var declaration in LayerDeclarationsByNamespace())
        {
            if (DcaLayout.IsBelow(ns, declaration.Namespace)
                && (nearest is null || declaration.Namespace.Length > nearest.Namespace.Length))
            {
                nearest = declaration;
            }
        }

        return nearest is null ? null : new Placement(nearest.Layer, nearest.Namespace, PlacementSource.Namespace);
    }

    /// <summary>
    /// The layer declarations of the loaded assemblies that name a namespace: an assembly attribute of a type the
    /// layout names for a layer whose first constructor argument is a string — the .NET stand-in for a layer
    /// declared on a <c>package-info</c>.
    /// </summary>
    private IReadOnlyList<NamespaceDeclaration> LayerDeclarationsByNamespace()
    {
        if (_namespaceDeclarations is null)
        {
            var declarations = new List<NamespaceDeclaration>();
            if (Layout.HasLayerAttributes)
            {
                foreach (var assembly in _assemblies)
                {
                    foreach (var data in AssemblyAttributes(assembly))
                    {
                        var layer = LayerAttributedBy(data.AttributeType.FullName);
                        if (layer is not null && NamedNamespace(data) is { } ns)
                        {
                            declarations.Add(new NamespaceDeclaration(ns, layer.Value, assembly.GetName().Name ?? ""));
                        }
                    }
                }
            }

            _namespaceDeclarations = declarations;
        }

        return _namespaceDeclarations;
    }

    /// <summary>The layer an assembly declares for all its types — a layer attribute naming no namespace — and the namespace its types share.</summary>
    private (DcaLayout.Layer Layer, string LayerNamespace)? AssemblyDeclaration(string assemblyFullName)
    {
        if (_assemblyDeclarations.TryGetValue(assemblyFullName, out var cached))
        {
            return cached;
        }

        (DcaLayout.Layer, string)? result = null;
        var assembly = _assemblies.FirstOrDefault(a => a.FullName == assemblyFullName);
        if (assembly is not null)
        {
            foreach (var data in AssemblyAttributes(assembly))
            {
                var layer = LayerAttributedBy(data.AttributeType.FullName);
                if (layer is not null && NamedNamespace(data) is null)
                {
                    result = (layer.Value, CommonNamespace(assembly));
                    break;
                }
            }
        }

        _assemblyDeclarations[assemblyFullName] = result;
        return result;
    }

    /// <summary>The longest namespace every type of the assembly below the root namespace lies in.</summary>
    private string CommonNamespace(Assembly assembly)
    {
        string? common = null;
        foreach (var type in RootTypes().Where(t => t.Assembly == assembly))
        {
            var ns = type.Namespace!;
            if (common is null)
            {
                common = ns;
                continue;
            }

            while (!DcaLayout.IsBelow(ns, common))
            {
                var dot = common.LastIndexOf('.');
                if (dot < 0)
                {
                    return Layout.RootNamespace;
                }

                common = common.Substring(0, dot);
            }
        }

        return common ?? Layout.RootNamespace;
    }

    private static IEnumerable<CustomAttributeData> AssemblyAttributes(Assembly assembly)
    {
        try
        {
            return assembly.GetCustomAttributesData();
        }
        catch (Exception e) when (e is TypeLoadException or FileNotFoundException or FileLoadException)
        {
            return Array.Empty<CustomAttributeData>();
        }
    }

    private static string? NamedNamespace(CustomAttributeData data) =>
        data.ConstructorArguments.Count > 0 && data.ConstructorArguments[0].Value is string ns && !string.IsNullOrWhiteSpace(ns)
            ? ns.Trim()
            : null;

    private DcaLayout.Layer? LayerAttributedBy(string? attributeName)
    {
        if (attributeName is null)
        {
            return null;
        }

        foreach (var layer in System.Enum.GetValues<DcaLayout.Layer>())
        {
            if (Layout.LayerAttributes(layer).Contains(attributeName))
            {
                return layer;
            }
        }

        return null;
    }

    /// <summary>The layer the path alone gives a namespace — what the <c>All…Patterns</c> have always meant.</summary>
    private Placement? PathPlacement(string? ns)
    {
        if (ns is null)
        {
            return null;
        }

        if (Layout.LayerHasNamespace(DcaLayout.Layer.Infrastructure))
        {
            foreach (var infrastructure in InfrastructureNamespaces())
            {
                if (DcaLayout.IsBelow(ns, infrastructure))
                {
                    return new Placement(DcaLayout.Layer.Infrastructure, infrastructure, PlacementSource.Path);
                }
            }
        }

        var root = ModuleRootOf(ns);
        if (root is null)
        {
            return null;
        }

        foreach (var layer in new[] { DcaLayout.Layer.Domain, DcaLayout.Layer.Application, DcaLayout.Layer.IncomingAdapter, DcaLayout.Layer.OutgoingAdapter })
        {
            var layerNamespace = root + "." + Layout.LayerNamespace(layer);
            if (Layout.LayerHasNamespace(layer) && DcaLayout.IsBelow(ns, layerNamespace))
            {
                return new Placement(layer, layerNamespace, PlacementSource.Path);
            }
        }

        return null;
    }

    /// <summary>
    /// The module root an attribute placed a type under: the bounded context that contains its layer namespace,
    /// else the layer namespace's parent. A type placed by its own attribute counts only inside a declared
    /// context — a lone type is no module.
    /// </summary>
    private string? AttributedModuleRoot(Placement placement)
    {
        var context = DeclaredContextOf(placement.LayerNamespace);
        if (context is not null)
        {
            return context;
        }

        if (placement.Source == PlacementSource.Type)
        {
            return null;
        }

        var dot = placement.LayerNamespace.LastIndexOf('.');
        return dot < 0 ? null : placement.LayerNamespace.Substring(0, dot);
    }

    /// <summary>The nearest namespace at or above <paramref name="ns"/> that declares a bounded context, or <c>null</c>.</summary>
    private string? DeclaredContextOf(string ns)
    {
        var root = Layout.RootNamespace;
        if (!DcaLayout.IsBelow(ns, root))
        {
            return null;
        }

        for (var candidate = ns; candidate is not null; candidate = ParentNamespace(candidate, root))
        {
            if (IsContextRoot(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>The module root a type belongs to — by its placement, else by its namespace; <c>null</c> for neither.</summary>
    public string? ModuleRootOf(IType type)
    {
        var placement = PlacementOf(type);
        if (placement is not null && placement.Source != PlacementSource.Path && AttributedModuleRoot(placement) is { } root)
        {
            return root;
        }

        return ModuleRootOf(type.Namespace?.FullName ?? "");
    }

    /// <summary>Whether the layout places the type in the given layer.</summary>
    public bool InLayer(IType type, DcaLayout.Layer layer) => PlacementOf(type)?.Layer == layer;

    /// <summary>Whether the layout places the reflected type in the given layer.</summary>
    public bool InLayer(Type type, DcaLayout.Layer layer) => PlacementOf(type)?.Layer == layer;

    /// <summary>Types the layout places in the given layer of the given module root.</summary>
    public Func<IType, bool> InLayerOf(DcaLayout.Layer layer, string moduleRoot) =>
        type => InLayer(type, layer) && moduleRoot == ModuleRootOf(type);

    /// <summary>Types in a sub-namespace of their layer's namespace — <c>&lt;layer namespace&gt;.&lt;segment&gt;</c> and below.</summary>
    public bool InLayerSegment(IType type, DcaLayout.Layer layer, string segment) =>
        PlacementOf(type) is { } placement
        && placement.Layer == layer
        && DcaLayout.IsBelow(type.Namespace?.FullName ?? "", placement.LayerNamespace + "." + segment);

    /// <summary>Whether the layout places the type in the domain layer.</summary>
    public bool InDomain(IType type) => InLayer(type, DcaLayout.Layer.Domain);

    /// <summary>The domain model: <c>&lt;domain layer namespace&gt;.&lt;Model&gt;</c> and below.</summary>
    public bool InDomainModel(IType type) => InLayerSegment(type, DcaLayout.Layer.Domain, Layout.ModelSegment);

    /// <summary>Whether the layout places the type in the application layer.</summary>
    public bool InApplication(IType type) => InLayer(type, DcaLayout.Layer.Application);

    /// <summary>The output ports shared by one context's use cases: <c>&lt;application&gt;.&lt;Shared&gt;</c> and below.</summary>
    public bool InSharedOutputPorts(IType type) => InLayerSegment(type, DcaLayout.Layer.Application, Layout.SharedSegment);

    /// <summary>Whether the layout places the type in the incoming adapters.</summary>
    public bool InIncomingAdapter(IType type) => InLayer(type, DcaLayout.Layer.IncomingAdapter);

    /// <summary>Whether the layout places the type in the outgoing adapters.</summary>
    public bool InOutgoingAdapter(IType type) => InLayer(type, DcaLayout.Layer.OutgoingAdapter);

    /// <summary>
    /// Types in either adapter layer, or below a module root's adapter namespace outside both — the
    /// <c>&lt;root&gt;.Adapter</c> and below the path rules have always meant.
    /// </summary>
    public bool InAdapter(IType type)
    {
        var placement = PlacementOf(type);
        if (placement is not null)
        {
            return placement.Layer is DcaLayout.Layer.IncomingAdapter or DcaLayout.Layer.OutgoingAdapter;
        }

        var ns = type.Namespace?.FullName ?? "";
        var root = ModuleRootOf(ns);
        return root is not null
            && LayerSegments().Contains(Layout.AdapterSegment)
            && DcaLayout.IsBelow(ns, root + "." + Layout.AdapterSegment);
    }

    /// <summary>Whether the layout places the type in the infrastructure.</summary>
    public bool InInfrastructure(IType type) => InLayer(type, DcaLayout.Layer.Infrastructure);

    /// <summary>
    /// Findings about the layout's layer declarations: a namespace whose attribute names another layer than its
    /// path, and a namespace that declares another layer than a namespace above it or than its assembly. Notices
    /// in the report, not failures — a migration keeps path and attribute side by side.
    /// </summary>
    public IReadOnlyList<string> LayerNotices()
    {
        if (!Layout.HasLayerAttributes)
        {
            return Array.Empty<string>();
        }

        var notices = new List<string>();
        foreach (var declaration in LayerDeclarationsByNamespace().OrderBy(d => d.Namespace, StringComparer.Ordinal))
        {
            var ns = declaration.Namespace;
            var declared = DcaLayout.RoleOf(declaration.Layer);
            var path = PathPlacement(ns);
            if (path is not null && path.Layer != declaration.Layer)
            {
                Add($"namespace {ns}: path says {DcaLayout.RoleOf(path.Layer)}, attribute says {declared}, {declared} holds");
            }

            var dot = ns.LastIndexOf('.');
            var above = dot < 0 ? null : NamespaceDeclarationOf(ns.Substring(0, dot));
            if (above is not null && above.Layer != declaration.Layer)
            {
                Add($"namespace {ns} declares {declared} inside {above.LayerNamespace}, which declares {DcaLayout.RoleOf(above.Layer)} - one layer per subtree");
            }
        }

        foreach (var assembly in _assemblies.OrderBy(a => a.FullName, StringComparer.Ordinal))
        {
            if (assembly.FullName is null || AssemblyDeclaration(assembly.FullName) is not { } declaration)
            {
                continue;
            }

            var declared = DcaLayout.RoleOf(declaration.Layer);
            foreach (var ns in RootTypes().Where(t => t.Assembly == assembly).Select(t => t.Namespace!).Distinct().OrderBy(n => n, StringComparer.Ordinal))
            {
                var path = PathPlacement(ns);
                if (NamespaceDeclarationOf(ns) is null && path is not null && path.Layer != declaration.Layer)
                {
                    Add($"namespace {ns}: path says {DcaLayout.RoleOf(path.Layer)}, attribute says {declared} on assembly {assembly.GetName().Name}, {declared} holds");
                }
            }

            foreach (var inner in LayerDeclarationsByNamespace().Where(d => d.AssemblyName == assembly.GetName().Name && d.Layer != declaration.Layer))
            {
                Add($"namespace {inner.Namespace} declares {DcaLayout.RoleOf(inner.Layer)} inside assembly {assembly.GetName().Name}, which declares {declared} - one layer per subtree");
            }
        }

        return notices;

        void Add(string notice)
        {
            if (!notices.Contains(notice))
            {
                notices.Add(notice);
            }
        }
    }

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
    /// <remarks>
    /// For <see cref="BoundedContextAttribute"/> and <see cref="SharedKernelAttribute"/> the attribute looked for
    /// is the one the layout's marker role names (<see cref="DcaMarkers.BoundedContext"/>,
    /// <see cref="DcaMarkers.SharedKernel"/>), matched by full type name; a project's own attribute is then
    /// answered as the library type, its members read by name (<see cref="NamespaceDeclarations"/>).
    /// </remarks>
    public IReadOnlyList<T> NamespaceAttributes<T>(string ns)
        where T : Attribute
    {
        var declared = DeclaredAttributeName(typeof(T));
        if (declared == typeof(T).FullName)
        {
            return RootTypes()
                .Where(t => t.Namespace == ns)
                .SelectMany(t => t.GetCustomAttributes<T>(inherit: false))
                .ToList();
        }

        return DeclaredAttributes(ns, declared)
            .Select(attribute => (T)NamespaceDeclarations.As(typeof(T), attribute, ns))
            .ToList();
    }

    /// <summary>Whether a marker class residing exactly in <paramref name="ns"/> carries the attribute of that full name.</summary>
    private bool HasNamespaceAttribute(string ns, string attributeName) => DeclaredAttributes(ns, attributeName).Any();

    private IEnumerable<object> DeclaredAttributes(string ns, string attributeName) =>
        RootTypes()
            .Where(t => t.Namespace == ns)
            .SelectMany(t => t.GetCustomAttributes(inherit: false))
            .Where(a => a.GetType().FullName == attributeName);

    /// <summary>The full name of the attribute the layout declares for the given library attribute.</summary>
    private string DeclaredAttributeName(Type attributeType)
    {
        if (attributeType == typeof(BoundedContextAttribute))
        {
            return Layout.Markers.BoundedContext;
        }

        return attributeType == typeof(SharedKernelAttribute) ? Layout.Markers.SharedKernel : attributeType.FullName!;
    }

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
                .Where(t => t.Namespace is not null
                    && DcaLayout.IsBelow(t.Namespace, Layout.RootNamespace)
                    && !Layout.IsExcluded(t.Namespace))
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
    public IReadOnlyList<string> InfrastructureNamespaces() => _infrastructureNamespaces ??= ComputeInfrastructureNamespaces();

    private IReadOnlyList<string> ComputeInfrastructureNamespaces()
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
