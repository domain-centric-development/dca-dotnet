using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DomainCentric.ArchRules.ContextMap;

/// <summary>
/// Renders the strategic context map of a <see cref="DcaArchitecture"/> as markdown — a fully derived
/// view of the <c>[BoundedContext]</c>, <c>[Upstream]</c>, <c>[ExternalUpstream]</c> and
/// <c>[Partnership]</c> attributes on the context marker classes.
/// </summary>
/// <remarks>
/// <para>Opt-in, not a rule: typical use is a test that regenerates <c>docs/context-map.md</c> and
/// fails when the committed file was stale.</para>
/// <code>
/// ContextMapRenderer.Of(arch).WithTitle("Context Map").WriteTo("docs/context-map.md");
/// </code>
/// <para>The document has the same structure as the one produced by the Java library
/// (<c>dca-archunit</c>): headings, table columns, ordering and Mermaid syntax are identical, so an
/// equivalent Java and .NET project render the same map.</para>
/// </remarks>
public sealed class ContextMapRenderer
{
    /// <summary>Published-interface channels, as namespace segments below the context root (matched case-insensitively).</summary>

    private readonly DcaArchitecture _arch;
    private bool _includeExternalSystems = true;
    private bool _includePlanned = true;
    private bool _withMermaid = true;
    private string _title = "Context Map";

    private ContextMapRenderer(DcaArchitecture arch)
    {
        _arch = arch ?? throw new ArgumentNullException(nameof(arch));
    }

    /// <summary>Starts rendering the context map of the given architecture with default options.</summary>
    public static ContextMapRenderer Of(DcaArchitecture arch) => new(arch);

    /// <summary>Whether <c>[ExternalUpstream]</c> declarations are rendered (default <c>true</c>).</summary>
    public ContextMapRenderer IncludeExternalSystems(bool value)
    {
        _includeExternalSystems = value;
        return this;
    }

    /// <summary>Whether relationships with status <see cref="UpstreamStatus.Planned"/> are rendered (default <c>true</c>).</summary>
    public ContextMapRenderer IncludePlanned(bool value)
    {
        _includePlanned = value;
        return this;
    }

    /// <summary>Whether the Mermaid diagram section is rendered (default <c>true</c>).</summary>
    public ContextMapRenderer WithMermaid(bool value)
    {
        _withMermaid = value;
        return this;
    }

    /// <summary>The level-one heading (default <c>"Context Map"</c>).</summary>
    public ContextMapRenderer WithTitle(string value)
    {
        _title = value ?? throw new ArgumentNullException(nameof(value));
        return this;
    }

    /// <summary>Writes the rendered markdown, creating parent directories and overwriting an existing file.</summary>
    public void WriteTo(string path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        var parent = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }

        File.WriteAllText(path, Render(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <summary>Renders the context map as markdown.</summary>
    public string Render()
    {
        var contexts = _arch.BoundedContexts;
        var namespaces = contexts.Keys.OrderBy(ShortName, StringComparer.Ordinal).ToList();

        var md = new StringBuilder();
        md.Append("# ").Append(_title).Append("\n\n");
        md.Append("> **Generated file — do not edit.** Derived from the `[BoundedContext]`, `[Upstream]`,\n");
        md.Append("> `[ExternalUpstream]`, and `[Partnership]` context attributes by\n");
        md.Append("> `ContextMapRenderer`. After changing a declaration, regenerate and commit this file.\n\n");
        md.Append("Each side declares only what it controls: the downstream declares its consumed upstreams\n");
        md.Append("(`[Upstream]`: translation strategy and channel), the upstream publishes its contract\n");
        md.Append("(`api`/`events` namespaces, `[OpenHostService]`), and partnerships are declared\n");
        md.Append("symmetrically on both contexts. Organizational patterns such as Customer–Supplier are not\n");
        md.Append("machine-classified; Separate Ways is the absence of any declaration. External systems\n");
        md.Append("appear via `[ExternalUpstream]` on their consuming context — the model dependency always\n");
        md.Append("points to the external system, regardless of who initiates the exchange. Non-context\n");
        md.Append("modules and the shared kernel are intentionally not part of this map.\n\n");

        md.Append("## Bounded Contexts\n\n");
        md.Append("| Module | Name | Description | Published interfaces |\n");
        md.Append("|---|---|---|---|\n");
        foreach (var ns in namespaces)
        {
            var published = PublishedInterfaces(ns);
            md.Append("| ").Append(ShortName(ns))
                .Append(" | ").Append(contexts[ns].Name)
                .Append(" | ").Append(contexts[ns].Description)
                .Append(" | ").Append(published.Count == 0 ? "—" : string.Join(", ", published))
                .Append(" |\n");
        }

        if (_withMermaid)
        {
            RenderDiagram(md, contexts, namespaces);
        }

        md.Append("\n## Upstream relationships\n\n");
        md.Append("| Downstream | Upstream | Channel | Translation | Status | Rationale |\n");
        md.Append("|---|---|---|---|---|---|\n");
        foreach (var ns in namespaces)
        {
            var source = ShortName(ns);
            foreach (var u in Upstreams(ns))
            {
                foreach (var channel in u.Via)
                {
                    md.Append("| ").Append(source)
                        .Append(" | ").Append(u.Context)
                        .Append(" | ").Append(ChannelName(channel))
                        .Append(" | ").Append(TranslationLabel(u.Translation))
                        .Append(" | ").Append(StatusName(u.Status))
                        .Append(" | ").Append(u.Rationale)
                        .Append(" |\n");
                }
            }
        }

        if (_includeExternalSystems)
        {
            md.Append("\n## External systems\n\n");
            var anyExternal = namespaces.Any(ns => ExternalUpstreams(ns).Count > 0);
            if (!anyExternal)
            {
                md.Append("None declared.\n");
            }
            else
            {
                md.Append("| Consumer | External system | Interaction | Protocol | Exchanges | Translation | Status | Rationale |\n");
                md.Append("|---|---|---|---|---|---|---|---|\n");
                foreach (var ns in namespaces)
                {
                    var source = ShortName(ns);
                    foreach (var e in ExternalUpstreams(ns))
                    {
                        md.Append("| ").Append(source)
                            .Append(" | ").Append(e.Name)
                            .Append(" | ").Append(InteractionName(e.Interaction))
                            .Append(" | ").Append(OrDash(e.Protocol))
                            .Append(" | ").Append(OrDash(e.Exchanges))
                            .Append(" | ").Append(TranslationLabel(e.Translation))
                            .Append(" | ").Append(StatusName(e.Status))
                            .Append(" | ").Append(e.Rationale)
                            .Append(" |\n");
                    }
                }
            }
        }

        md.Append("\n## Partnerships\n\n");
        var pairs = PartnershipPairs(namespaces);
        if (pairs.Count == 0)
        {
            md.Append("None declared.\n");
        }
        else
        {
            md.Append("| Contexts | Rationale |\n");
            md.Append("|---|---|\n");
            foreach (var (pair, rationales) in pairs)
            {
                md.Append("| ").Append(pair.First).Append(" ↔ ").Append(pair.Second)
                    .Append(" | ").Append(string.Join(" — ", rationales)).Append(" |\n");
            }
        }

        return md.ToString();
    }

    private void RenderDiagram(StringBuilder md, IReadOnlyDictionary<string, BoundedContextAttribute> contexts, List<string> namespaces)
    {
        md.Append("\n## Diagram\n\n");
        md.Append("```mermaid\ngraph LR\n");
        foreach (var ns in namespaces)
        {
            md.Append("  ").Append(ShortName(ns)).Append("[\"").Append(contexts[ns].Name).Append(PublishedBadge(ns)).Append("\"]\n");
        }

        md.Append('\n');
        foreach (var ns in namespaces)
        {
            var source = ShortName(ns);
            foreach (var u in Upstreams(ns))
            {
                foreach (var channel in u.Via)
                {
                    var label = TranslationLabel(u.Translation) + " / " + ChannelName(channel) + StatusSuffix(u.Status);
                    var arrow = channel == Consumes.Api ? "-->" : "-.->";
                    md.Append("  ").Append(source).Append(' ').Append(arrow).Append("|\"").Append(label).Append("\"| ").Append(u.Context).Append('\n');
                }
            }
        }

        if (_includeExternalSystems)
        {
            var externalIds = new Dictionary<string, string>(StringComparer.Ordinal);
            var usedIds = new HashSet<string>(namespaces.Select(ShortName), StringComparer.Ordinal);
            foreach (var name in ExternalSystems(namespaces)) {
                var stem = ExternalId(name); var id = stem; var suffix = 2;
                while (!usedIds.Add(id)) id = stem + "_" + suffix++;
                externalIds.Add(name, id);
            }
            foreach (var name in ExternalSystems(namespaces))
            {
                md.Append("  ").Append(externalIds[name]).Append("[[\"").Append(name).Append("\"]]\n");
            }

            foreach (var ns in namespaces)
            {
                var source = ShortName(ns);
                foreach (var e in ExternalUpstreams(ns))
                {
                    // The one-word protocol replaces the generic inbound/outbound in the label — the arrow
                    // style already encodes the direction. The full exchanges text lives in the table only.
                    var kind = e.Protocol.Length == 0 ? InteractionName(e.Interaction) : e.Protocol;
                    var label = TranslationLabel(e.Translation) + " / " + kind + StatusSuffix(e.Status);
                    var arrow = e.Interaction == Interaction.Outbound ? "-->" : "-.->";
                    md.Append("  ").Append(source).Append(' ').Append(arrow).Append("|\"").Append(label).Append("\"| ").Append(externalIds[e.Name]).Append('\n');
                }
            }
        }

        foreach (var (pair, _) in PartnershipPairs(namespaces))
        {
            md.Append("  ").Append(pair.First).Append(" ---|\"Partnership\"| ").Append(pair.Second).Append('\n');
        }

        md.Append("```\n\n");
        md.Append("Arrows point from downstream to upstream (dependency direction, never call direction).\n");
        md.Append("Solid arrows are synchronous consumption (`api` / external `outbound`), dotted arrows are\n");
        md.Append("asynchronous consumption (`events` / external `inbound`), plain lines are partnerships.\n");
        md.Append("Double-framed nodes are external systems. Node badges list published interfaces.\n");
        md.Append("Edges labeled `planned` are declared intent without a code dependency yet.\n");
    }

    // ---------------------------------------------------------------------------------------------
    // Declarations (filtered by options)
    // ---------------------------------------------------------------------------------------------

    private IReadOnlyList<UpstreamAttribute> Upstreams(string ns) =>
        _arch.NamespaceAttributes<UpstreamAttribute>(ns).Where(u => _includePlanned || u.Status != UpstreamStatus.Planned).ToList();

    private IReadOnlyList<ExternalUpstreamAttribute> ExternalUpstreams(string ns) =>
        _arch.NamespaceAttributes<ExternalUpstreamAttribute>(ns).Where(e => _includePlanned || e.Status != UpstreamStatus.Planned).ToList();

    /// <summary>Deduplicated symmetric pairs (sorted) with the distinct rationales of both sides, in encounter order.</summary>
    private List<((string First, string Second) Pair, List<string> Rationales)> PartnershipPairs(List<string> namespaces)
    {
        var pairs = new List<((string First, string Second) Pair, List<string> Rationales)>();
        foreach (var ns in namespaces)
        {
            var source = ShortName(ns);
            foreach (var p in _arch.NamespaceAttributes<PartnershipAttribute>(ns))
            {
                var pair = string.CompareOrdinal(source, p.Context) <= 0 ? (source, p.Context) : (p.Context, source);
                var index = pairs.FindIndex(x => x.Pair == pair);
                if (index < 0)
                {
                    pairs.Add((pair, new List<string>()));
                    index = pairs.Count - 1;
                }

                var rationales = pairs[index].Rationales;
                if (p.Rationale.Length > 0 && !rationales.Contains(p.Rationale))
                {
                    rationales.Add(p.Rationale);
                }
            }
        }

        return pairs;
    }

    /// <summary>All declared external system names, sorted for deterministic output.</summary>
    private List<string> ExternalSystems(List<string> namespaces) =>
        namespaces.SelectMany(ExternalUpstreams).Select(e => e.Name).Distinct().OrderBy(n => n, StringComparer.Ordinal).ToList();

    /// <summary>
    /// Published interfaces ("api", "events") of a context: the channel namespace directly below the
    /// context root (<c>Cart.Api</c>, <c>Cart.Events</c>) carries types. .NET has no named-interface
    /// declaration (the Java twin reads one from its module system), so type presence stands alone.
    /// </summary>
    private List<string> PublishedInterfaces(string contextNamespace)
    {
        var published = new List<string>();
        foreach (var channel in _arch.Layout.PublishedSegments)
        {
            // Exact namespace-segment boundary — a plain prefix would also match "Apiary"/"EventSourcing".
            var root = contextNamespace + "." + channel;
            var hasTypes = _arch.Types.Any(t =>
                t.Namespace.FullName.Equals(root, StringComparison.OrdinalIgnoreCase) ||
                t.Namespace.FullName.StartsWith(root + ".", StringComparison.OrdinalIgnoreCase));
            if (hasTypes)
            {
                published.Add(channel.ToLowerInvariant());
            }
        }

        return published;
    }

    /// <summary>Published interfaces of a context, shown as a node badge ("api", "events").</summary>
    private string PublishedBadge(string contextNamespace)
    {
        var published = PublishedInterfaces(contextNamespace);
        return published.Count == 0 ? "" : "<br/><i>" + string.Join(" · ", published) + "</i>";
    }

    // ---------------------------------------------------------------------------------------------
    // Labels
    // ---------------------------------------------------------------------------------------------

    /// <summary>Deterministic mermaid node id for an external system name.</summary>
    private static string ExternalId(string name) =>
        "ext_" + Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9]+", "_");

    private static string InteractionName(Interaction interaction) =>
        interaction == Interaction.Outbound ? "outbound" : "inbound";

    private static string TranslationLabel(Translation translation) =>
        translation == Translation.AntiCorruptionLayer ? "ACL" : "Conformist";

    private static string StatusName(UpstreamStatus status) =>
        status == UpstreamStatus.Planned ? "planned" : "implemented";

    /// <summary>Edge-label suffix marking planned relationships; empty for implemented ones.</summary>
    private static string StatusSuffix(UpstreamStatus status) =>
        status == UpstreamStatus.Planned ? " / planned" : "";

    /// <summary>Channel label in the rendered map: the layout's published segment, lower-cased (<c>api</c> / <c>events</c>).</summary>
    private string ChannelName(Consumes channel) =>
        (channel == Consumes.Api ? _arch.Layout.ApiSegment : _arch.Layout.EventsSegment).ToLowerInvariant();

    private static string OrDash(string value) => value.Length == 0 ? "—" : value;

    private string ShortName(string ns) => _arch.ContextName(ns);
}
