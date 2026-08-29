using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using Attribute = System.Attribute;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DomainCentric.ArchRules.Rules;

/// <summary>
/// Rules for the executable Context Map.
/// </summary>
/// <remarks>
/// <para>The context map is declared as attributes on the context marker classes, each side declaring
/// only what it controls: <c>[Upstream]</c> / <c>[ExternalUpstream]</c> on the downstream side,
/// <c>[Partnership]</c> on both sides, <c>[OpenHostService]</c> on the upstream side. These rules prove
/// the declarations consistent with each other and with the actual code. Organizational patterns
/// (Customer–Supplier etc.) are deliberately not machine-classified; Separate Ways is the absence of
/// any declaration.</para>
/// <para>The Java rule <c>DCA-MAP-006</c> (agreement with Spring Modulith <c>allowedDependencies</c>)
/// has no .NET counterpart — see <see cref="NotApplicable"/>.</para>
/// </remarks>
public sealed class ContextMapRules : IDcaRuleSet
{
    /// <summary>Java rules of this set that have no .NET counterpart (id → reason).</summary>
    public static readonly IReadOnlyDictionary<string, string> NotApplicable = new Dictionary<string, string>
    {
        ["DCA-MAP-006"] = "Upstream declarations and Spring Modulith allowedDependencies must agree — .NET has no module"
            + " system annotation; project boundaries take that role",
    };

    public ContextMapRules(DcaLayout layout)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Rules = new IDcaRule[]
        {
            DeclarationsOnlyOnBoundedContexts(),
            ExternalUpstreamsWellFormed(),
            ExternalSystemNamesDistinctAfterNormalization(),
            UpstreamsReferenceExistingContexts(),
            UpstreamsUniquePerContextAndChannel(),
            ImplementedUpstreamsBackedByCode(),
            AntiCorruptionLayerStaysInAdapter(),
            ConformistNeverReachesDomain(),
            ExternalContractTypesRespectTranslation(),
            CrossContextDependenciesRequireDeclaration(),
            PartnershipsSymmetric(),
            DisplayDeclaredContextMap(),
        };
    }

    public string Name => "contextmap";

    public IReadOnlyList<IDcaRule> Rules { get; }

    /// <summary>The layout this rule set was built for.</summary>
    public DcaLayout Layout { get; }

    // ---------------------------------------------------------------------------------------------
    // Declaration well-formedness
    // ---------------------------------------------------------------------------------------------

    /// <summary>DCA-MAP-001.</summary>
    public static IDcaRule DeclarationsOnlyOnBoundedContexts() =>
        DcaRule.Check(
            "DCA-MAP-001",
            "Upstream, ExternalUpstream, and Partnership may only be declared on bounded context namespaces",
            "Context map declarations are reserved for bounded contexts — only a context can be downstream of,"
                + " or partner with, another",
            arch =>
            {
                var violations = new List<string>();
                foreach (var ns in AllRootNamespaces(arch))
                {
                    if (arch.NamespaceAttribute<BoundedContextAttribute>(ns) is not null)
                    {
                        continue;
                    }
                    RequireNoDeclaration<UpstreamAttribute>(arch, ns, "[Upstream]", violations);
                    RequireNoDeclaration<ExternalUpstreamAttribute>(arch, ns, "[ExternalUpstream]", violations);
                    RequireNoDeclaration<PartnershipAttribute>(arch, ns, "[Partnership]", violations);
                }
                DcaRule.Fail("Context map declarations are reserved for bounded contexts", violations);
            });

    private static void RequireNoDeclaration<T>(DcaArchitecture arch, string ns, string label, List<string> violations)
        where T : Attribute
    {
        if (arch.NamespaceAttributes<T>(ns).Count > 0)
        {
            violations.Add("Namespace '" + ns + "' declares " + label
                + " but is not a [BoundedContext] — context map declarations are reserved for bounded contexts");
        }
    }

    /// <summary>DCA-MAP-002.</summary>
    public static IDcaRule ExternalUpstreamsWellFormed() =>
        DcaRule.Check(
            "DCA-MAP-002",
            "ExternalUpstream declarations must be well-formed and unique per name and interaction",
            "The identity of an [ExternalUpstream] declaration is (name, interaction); internal contexts are"
                + " declared with [Upstream] instead",
            arch =>
            {
                var violations = new List<string>();
                var moduleNames = ModuleNames(arch);
                foreach (var ns in arch.BoundedContextNamespaces)
                {
                    var source = ShortName(ns);
                    var edges = new List<string>();
                    foreach (var e in arch.NamespaceAttributes<ExternalUpstreamAttribute>(ns))
                    {
                        if (string.IsNullOrWhiteSpace(e.Name))
                        {
                            violations.Add("Context '" + source + "' declares an [ExternalUpstream] with a blank name");
                        }
                        if (moduleNames.Contains(e.Name))
                        {
                            violations.Add("Context '" + source + "' declares external system '" + e.Name
                                + "', which is an internal bounded context module — use [Upstream] for internal contexts");
                        }
                        var edge = e.Name + " :: " + e.Interaction;
                        if (edges.Contains(edge))
                        {
                            violations.Add("Context '" + source + "' declares external system edge '" + edge
                                + "' more than once — the identity of an [ExternalUpstream] declaration is (name, interaction)");
                        }
                        edges.Add(edge);
                    }
                }
                DcaRule.Fail("ExternalUpstream declarations must be well-formed and unique per name and interaction", violations);
            });

    /// <summary>DCA-MAP-003.</summary>
    public static IDcaRule ExternalSystemNamesDistinctAfterNormalization() =>
        DcaRule.Check(
            "DCA-MAP-003",
            "Distinct external system names must not collide after mermaid id normalization",
            "The generated context map renders one node per normalized external system name — two spellings of the"
                + " same system would silently merge into one node",
            arch =>
            {
                var violations = new List<string>();
                var idToName = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var ns in arch.BoundedContextNamespaces)
                {
                    foreach (var e in arch.NamespaceAttributes<ExternalUpstreamAttribute>(ns))
                    {
                        var id = NormalizedExternalId(e.Name);
                        var known = idToName.TryGetValue(id, out var k) ? k : e.Name;
                        if (known != e.Name)
                        {
                            violations.Add("External system names '" + known + "' and '" + e.Name
                                + "' normalize to the same mermaid node id '" + id + "' — use one canonical spelling");
                        }
                        idToName[id] = e.Name;
                    }
                }
                DcaRule.Fail("Distinct external system names must not collide after mermaid id normalization", violations);
            });

    /// <summary>DCA-MAP-004.</summary>
    public static IDcaRule UpstreamsReferenceExistingContexts() =>
        DcaRule.Check(
            "DCA-MAP-004",
            "Upstream declarations must reference an existing bounded context and never the declaring context itself",
            "A dangling or self-referencing upstream edge describes a relationship that cannot exist",
            arch =>
            {
                var violations = new List<string>();
                var moduleNames = ModuleNames(arch);
                foreach (var ns in arch.BoundedContextNamespaces)
                {
                    var source = ShortName(ns);
                    foreach (var u in arch.NamespaceAttributes<UpstreamAttribute>(ns))
                    {
                        if (!moduleNames.Contains(u.Context))
                        {
                            violations.Add("Context '" + source + "' declares [Upstream(\"" + u.Context
                                + "\")] but no bounded context module with that name exists (known: "
                                + string.Join(", ", moduleNames) + ")");
                        }
                        if (u.Context == source)
                        {
                            violations.Add("Context '" + source + "' declares itself as its own upstream");
                        }
                    }
                }
                DcaRule.Fail("Upstream declarations must reference an existing bounded context", violations);
            });

    /// <summary>DCA-MAP-005.</summary>
    public static IDcaRule UpstreamsUniquePerContextAndChannel() =>
        DcaRule.Check(
            "DCA-MAP-005",
            "Upstream declarations must be unique per context and channel, and via must not be empty",
            "The identity of an [Upstream] declaration is (context, via); different translations per channel"
                + " require separate attributes",
            arch =>
            {
                var violations = new List<string>();
                foreach (var ns in arch.BoundedContextNamespaces)
                {
                    var source = ShortName(ns);
                    var edges = new List<string>();
                    foreach (var u in arch.NamespaceAttributes<UpstreamAttribute>(ns))
                    {
                        if (u.Via.Count == 0)
                        {
                            violations.Add("Context '" + source + "': [Upstream(\"" + u.Context
                                + "\")] declares no channel — via must not be empty");
                        }
                        foreach (var channel in u.Via)
                        {
                            var edge = u.Context + " :: " + ChannelName(channel);
                            if (edges.Contains(edge))
                            {
                                violations.Add("Context '" + source + "' declares (context, channel) '" + edge
                                    + "' more than once — the identity of an [Upstream] declaration is (context, via);"
                                    + " different translations per channel require separate attributes");
                            }
                            edges.Add(edge);
                        }
                    }
                }
                DcaRule.Fail("Upstream declarations must be unique per context and channel", violations);
            });

    // ---------------------------------------------------------------------------------------------
    // Consistency with the code
    // ---------------------------------------------------------------------------------------------

    /// <summary>DCA-MAP-007.</summary>
    public static IDcaRule ImplementedUpstreamsBackedByCode() =>
        DcaRule.Check(
            "DCA-MAP-007",
            "Implemented Upstream declarations must be backed by an actual code dependency",
            "A declared Implemented edge without any real dependency is stale (or premature — then it is Planned) and"
                + " would otherwise pass forever alongside an equally stale module boundary entry",
            arch =>
            {
                var violations = new List<string>();
                var namespacesByName = NamespacesByName(arch);
                foreach (var ns in arch.BoundedContextNamespaces)
                {
                    var source = ShortName(ns);
                    foreach (var u in arch.NamespaceAttributes<UpstreamAttribute>(ns))
                    {
                        if (u.Status != UpstreamStatus.Implemented || !namespacesByName.TryGetValue(u.Context, out var targetNs))
                        {
                            continue;
                        }
                        foreach (var channel in u.Via)
                        {
                            var channelNs = targetNs + "." + ChannelName(channel);
                            var exists = TypesBelow(arch, ns).Any(t => DependsOnNamespace(t, channelNs));
                            if (!exists)
                            {
                                violations.Add("Context '" + source + "' declares [Upstream(\"" + u.Context + "\", via = "
                                    + ChannelName(channel) + ")] as Implemented, but no type in '" + ns + "' depends on '"
                                    + channelNs + "' — implement the dependency, mark the declaration Status = Planned, or remove it");
                            }
                        }
                    }
                }
                DcaRule.Fail("Implemented Upstream declarations must be backed by an actual code dependency", violations);
            });

    // ---------------------------------------------------------------------------------------------
    // Translation enforcement (channel-dependent)
    // ---------------------------------------------------------------------------------------------

    /// <summary>DCA-MAP-008.</summary>
    public IDcaRule AntiCorruptionLayerStaysInAdapter() =>
        DcaRule.Check(
            "DCA-MAP-008",
            "Anti-Corruption Layer: upstream contract types must stay inside the matching adapter",
            "The ACL sits where the dependency crosses the boundary — outgoing adapters for synchronous API calls,"
                + " incoming adapters for consumed events — and translates the upstream contract into the context's"
                + " own model there",
            arch =>
            {
                var violations = new List<string>();
                var namespacesByName = NamespacesByName(arch);
                foreach (var ns in arch.BoundedContextNamespaces)
                {
                    var source = ShortName(ns);
                    foreach (var u in arch.NamespaceAttributes<UpstreamAttribute>(ns))
                    {
                        if (u.Translation != Translation.AntiCorruptionLayer || !namespacesByName.TryGetValue(u.Context, out var targetNs))
                        {
                            continue;
                        }
                        foreach (var channel in u.Via)
                        {
                            var allowedAdapter = channel == Consumes.Api
                                ? OutgoingAdapterNamespace(ns)
                                : IncomingAdapterNamespace(ns);
                            var channelNs = targetNs + "." + ChannelName(channel);
                            foreach (var type in TypesBelow(arch, ns).Where(t => !IsBelow(t, allowedAdapter)))
                            {
                                if (DependsOnNamespace(type, channelNs))
                                {
                                    violations.Add("Context '" + source + "' declares AntiCorruptionLayer towards '" + u.Context
                                        + "' (" + ChannelName(channel) + ") — " + type.FullName + " uses upstream contract types"
                                        + " outside " + allowedAdapter + "; translate them there into the context's own model");
                                }
                            }
                        }
                    }
                }
                DcaRule.Fail("Anti-Corruption Layer: upstream contract types must stay inside the matching adapter", violations);
            });

    /// <summary>DCA-MAP-009.</summary>
    public IDcaRule ConformistNeverReachesDomain() =>
        DcaRule.Check(
            "DCA-MAP-009",
            "Conformist: upstream contract types must never reach the domain layer",
            "Conformism does not suspend domain purity — the domain layer stays free of foreign contract types",
            arch =>
            {
                var violations = new List<string>();
                var namespacesByName = NamespacesByName(arch);
                foreach (var ns in arch.BoundedContextNamespaces)
                {
                    var source = ShortName(ns);
                    foreach (var u in arch.NamespaceAttributes<UpstreamAttribute>(ns))
                    {
                        if (u.Translation != Translation.Conformist || !namespacesByName.TryGetValue(u.Context, out var targetNs))
                        {
                            continue;
                        }
                        foreach (var channel in u.Via)
                        {
                            var channelNs = targetNs + "." + ChannelName(channel);
                            foreach (var type in TypesBelow(arch, DomainNamespace(ns)))
                            {
                                if (DependsOnNamespace(type, channelNs))
                                {
                                    violations.Add("Context '" + source + "' conforms to '" + u.Context + "' (" + ChannelName(channel)
                                        + "), but conformism does not suspend domain purity — " + type.FullName
                                        + " in the domain layer uses foreign contract types");
                                }
                            }
                        }
                    }
                }
                DcaRule.Fail("Conformist: upstream contract types must never reach the domain layer", violations);
            });

    /// <summary>DCA-MAP-010.</summary>
    public IDcaRule ExternalContractTypesRespectTranslation() =>
        DcaRule.Check(
            "DCA-MAP-010",
            "External system contract types must respect the declared translation and interaction",
            "An external system's contract types are confined to the adapter where the exchange crosses the boundary"
                + " (ACL) or at least kept out of the domain (Conformist)",
            arch =>
            {
                // Without ContractNamespaces (wire-level contract, no vendor SDK) there is nothing to check —
                // the declaration then only documents the relationship.
                var violations = new List<string>();
                foreach (var ns in arch.BoundedContextNamespaces)
                {
                    var source = ShortName(ns);
                    foreach (var e in arch.NamespaceAttributes<ExternalUpstreamAttribute>(ns))
                    {
                        if (e.ContractNamespaces.Length == 0)
                        {
                            continue;
                        }
                        if (e.Translation == Translation.AntiCorruptionLayer)
                        {
                            var allowedAdapter = e.Interaction == Interaction.Outbound
                                ? OutgoingAdapterNamespace(ns)
                                : IncomingAdapterNamespace(ns);
                            foreach (var type in TypesBelow(arch, ns).Where(t => !IsBelow(t, allowedAdapter)))
                            {
                                if (e.ContractNamespaces.Any(c => DependsOnNamespace(type, c)))
                                {
                                    violations.Add("Context '" + source + "' declares AntiCorruptionLayer towards external system '"
                                        + e.Name + "' (" + e.Interaction + ") — " + type.FullName + " uses its contract types ("
                                        + string.Join(", ", e.ContractNamespaces) + ") outside " + allowedAdapter);
                                }
                            }
                        }
                        else
                        {
                            foreach (var type in TypesBelow(arch, DomainNamespace(ns)))
                            {
                                if (e.ContractNamespaces.Any(c => DependsOnNamespace(type, c)))
                                {
                                    violations.Add("Context '" + source + "' conforms to external system '" + e.Name
                                        + "', but conformism does not suspend domain purity — " + type.FullName
                                        + " in the domain layer uses its contract types");
                                }
                            }
                        }
                    }
                }
                DcaRule.Fail("External system contract types must respect the declared translation and interaction", violations);
            });

    /// <summary>DCA-MAP-011.</summary>
    public static IDcaRule CrossContextDependenciesRequireDeclaration() =>
        DcaRule.Check(
            "DCA-MAP-011",
            "Cross-context dependencies on published interfaces require an Upstream declaration",
            "Every real dependency on a foreign Api/ or Events/ namespace is a context-map edge and must be declared as such",
            arch =>
            {
                var violations = new List<string>();
                var contexts = arch.BoundedContextNamespaces;
                foreach (var srcNs in contexts)
                {
                    var source = ShortName(srcNs);
                    var declared = DeclaredEdges(arch, srcNs);
                    var sourceTypes = TypesBelow(arch, srcNs).ToList();
                    foreach (var tgtNs in contexts)
                    {
                        if (tgtNs == srcNs)
                        {
                            continue;
                        }
                        var target = ShortName(tgtNs);
                        foreach (var channel in Channels)
                        {
                            if (declared.Contains(target + " :: " + channel))
                            {
                                continue;
                            }
                            var channelNs = tgtNs + "." + channel;
                            foreach (var type in sourceTypes.Where(t => DependsOnNamespace(t, channelNs)))
                            {
                                violations.Add("Context '" + source + "' depends on '" + target + " :: " + channel
                                    + "' without declaring it (" + type.FullName + ") — add [Upstream(\"" + target
                                    + "\", Translation..., Consumes...)] to its context marker class");
                            }
                        }
                    }
                }
                DcaRule.Fail("Cross-context dependencies on published interfaces require an Upstream declaration", violations);
            });

    // ---------------------------------------------------------------------------------------------
    // Partnership symmetry
    // ---------------------------------------------------------------------------------------------

    /// <summary>DCA-MAP-012.</summary>
    public static IDcaRule PartnershipsSymmetric() =>
        DcaRule.Check(
            "DCA-MAP-012",
            "Partnership declarations must reference an existing bounded context, never themselves, and must be symmetric",
            "A partnership is a mutual commitment — it exists only when both contexts declare it",
            arch =>
            {
                var violations = new List<string>();
                var namespacesByName = NamespacesByName(arch);
                foreach (var ns in arch.BoundedContextNamespaces)
                {
                    var source = ShortName(ns);
                    foreach (var p in arch.NamespaceAttributes<PartnershipAttribute>(ns))
                    {
                        if (!namespacesByName.TryGetValue(p.Context, out var partnerNs))
                        {
                            violations.Add("Context '" + source + "' declares [Partnership(\"" + p.Context
                                + "\")] but no bounded context module with that name exists");
                            continue;
                        }
                        if (p.Context == source)
                        {
                            violations.Add("Context '" + source + "' declares a partnership with itself");
                            continue;
                        }
                        var reverse = arch.NamespaceAttributes<PartnershipAttribute>(partnerNs).Any(r => r.Context == source);
                        if (!reverse)
                        {
                            violations.Add("Partnership between '" + source + "' and '" + p.Context + "' is only declared on '"
                                + source + "' — partnerships are symmetric, add [Partnership(\"" + source + "\")] to '"
                                + p.Context + "'");
                        }
                    }
                }
                DcaRule.Fail("Partnership declarations must be symmetric and reference existing contexts", violations);
            });

    // ---------------------------------------------------------------------------------------------
    // Diagnostic
    // ---------------------------------------------------------------------------------------------

    /// <summary>DCA-MAP-013 — never fails.</summary>
    public static IDcaRule DisplayDeclaredContextMap() =>
        DcaRule.Check(
            "DCA-MAP-013",
            "Diagnostic: Display declared context map",
            "Printing the declared edges makes the executable context map reviewable at a glance",
            arch =>
            {
                Console.WriteLine("=== Context Map (declared) ===");
                foreach (var ns in arch.BoundedContextNamespaces)
                {
                    var source = ShortName(ns);
                    foreach (var u in arch.NamespaceAttributes<UpstreamAttribute>(ns))
                    {
                        foreach (var channel in u.Via)
                        {
                            Console.WriteLine("  " + source + " --[" + u.Translation + " / " + ChannelName(channel) + "]--> " + u.Context);
                        }
                    }
                    foreach (var e in arch.NamespaceAttributes<ExternalUpstreamAttribute>(ns))
                    {
                        Console.WriteLine("  " + source + " --[" + e.Translation + " / " + e.Interaction + "]--> (external) " + e.Name);
                    }
                    foreach (var p in arch.NamespaceAttributes<PartnershipAttribute>(ns))
                    {
                        Console.WriteLine("  " + source + " <--[Partnership]--> " + p.Context);
                    }
                }
                Console.WriteLine("==============================");
            });

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    private static readonly string[] Channels = { ChannelName(Consumes.Api), ChannelName(Consumes.Events) };

    private static IEnumerable<string> AllRootNamespaces(DcaArchitecture arch)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in arch.RuntimeTypes())
        {
            var root = arch.RootContextNamespace(type.Namespace!);
            if (root is not null && seen.Add(root))
            {
                yield return root;
            }
        }
    }

    private static HashSet<string> ModuleNames(DcaArchitecture arch) =>
        new(arch.BoundedContextNamespaces.Select(ShortName), StringComparer.Ordinal);

    private static Dictionary<string, string> NamespacesByName(DcaArchitecture arch) =>
        arch.BoundedContextNamespaces.ToDictionary(ShortName, ns => ns, StringComparer.Ordinal);

    /// <summary>All declared upstream edges of a context as "target :: channel" strings.</summary>
    private static HashSet<string> DeclaredEdges(DcaArchitecture arch, string contextNamespace) =>
        new(
            arch.NamespaceAttributes<UpstreamAttribute>(contextNamespace)
                .SelectMany(u => u.Via.Select(c => u.Context + " :: " + ChannelName(c))),
            StringComparer.Ordinal);

    /// <summary>The published-interface namespace segment of a channel (<c>Api</c> / <c>Events</c>).</summary>
    private static string ChannelName(Consumes channel) => channel.ToString();

    private string DomainNamespace(string contextNamespace) => contextNamespace + "." + Layout.DomainSegment;

    private string OutgoingAdapterNamespace(string contextNamespace) =>
        contextNamespace + "." + Layout.AdapterSegment + "." + Layout.OutgoingSegment;

    private string IncomingAdapterNamespace(string contextNamespace) =>
        contextNamespace + "." + Layout.AdapterSegment + "." + Layout.IncomingSegment;

    private static IEnumerable<IType> TypesBelow(DcaArchitecture arch, string ns) =>
        arch.Types.Where(t => IsBelow(t, ns));

    private static bool IsBelow(IType type, string ns) =>
        type.Namespace is not null && DcaLayout.IsBelow(type.Namespace.FullName, ns);

    /// <summary>True when the type has at least one direct dependency on a type in <paramref name="ns"/> or below.</summary>
    private static bool DependsOnNamespace(IType type, string ns) =>
        type.Dependencies.Any(d =>
            d.Target.Namespace is not null
            && !ReferenceEquals(d.Target, type)
            && DcaLayout.IsBelow(d.Target.Namespace.FullName, ns));

    /// <summary>The mermaid node id of an external system in the generated context map.</summary>
    private static string NormalizedExternalId(string name) =>
        "ext_" + Regex.Replace(name.ToLower(CultureInfo.InvariantCulture), "[^a-z0-9]+", "_");

    private static string ShortName(string contextNamespace) => DcaArchitecture.SimpleContextName(contextNamespace);
}
