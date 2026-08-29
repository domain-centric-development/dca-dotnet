using System;
using System.IO;
using System.Linq;
using DomainCentric.ArchRules.ContextMap;
using Xunit;

namespace DomainCentric.ArchRules.Tests.ContextMap;

public sealed class ContextMapRendererTests
{
    private const string Root = "DomainCentric.ArchRules.Tests.Fixtures.ContextMapRender";

    private static readonly DcaArchitecture Arch =
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(Root), typeof(ContextMapRendererTests).Assembly);

    private static string[] Lines(string md) => md.Split('\n');

    [Fact]
    public void RendersBoundedContextTableSortedByModule()
    {
        var lines = Lines(ContextMapRenderer.Of(Arch).Render());
        Assert.Equal("# Context Map", lines[0]);
        var cart = Array.IndexOf(lines, "| Cart | Shopping Cart | Carts of guests and customers | — |");
        var catalog = Array.IndexOf(lines, "| Catalog | Product Catalog | Master data of sellable products | api |");
        var shipping = Array.IndexOf(lines, "| Shipping | Shipping | Parcel dispatch | — |");
        Assert.True(cart > 0 && catalog > cart && shipping > catalog, string.Join("\n", lines));
    }

    [Fact]
    public void RendersUpstreamsExternalSystemsAndPartnerships()
    {
        var md = ContextMapRenderer.Of(Arch).Render();
        Assert.Contains("| Cart | Catalog | api | ACL | implemented | Cart needs product master data |", md, StringComparison.Ordinal);
        Assert.Contains("| Cart | Catalog | events | ACL | implemented | Cart needs product master data |", md, StringComparison.Ordinal);
        Assert.Contains("| Shipping | Cart | events | Conformist | planned | Ship what was ordered |", md, StringComparison.Ordinal);
        Assert.Contains("| Shipping | Carrier API | outbound | REST | Shipment labels | ACL | implemented | Labels are printed by the carrier |", md, StringComparison.Ordinal);
        Assert.Contains("| Cart ↔ Catalog | Catalog and cart evolve together |", md, StringComparison.Ordinal);
        Assert.Equal(1, Lines(md).Count(l => l.StartsWith("| Cart ↔ Catalog", StringComparison.Ordinal)));
    }

    [Fact]
    public void RendersMermaidDiagram()
    {
        var md = ContextMapRenderer.Of(Arch).Render();
        Assert.Contains("```mermaid\ngraph LR\n", md, StringComparison.Ordinal);
        Assert.Contains("  Catalog[\"Product Catalog<br/><i>api</i>\"]", md, StringComparison.Ordinal);
        Assert.Contains("  Cart -->|\"ACL / api\"| Catalog", md, StringComparison.Ordinal);
        Assert.Contains("  Cart -.->|\"ACL / events\"| Catalog", md, StringComparison.Ordinal);
        Assert.Contains("  Shipping -.->|\"Conformist / events / planned\"| Cart", md, StringComparison.Ordinal);
        Assert.Contains("  ext_carrier_api[[\"Carrier API\"]]", md, StringComparison.Ordinal);
        Assert.Contains("  Shipping -->|\"ACL / REST\"| ext_carrier_api", md, StringComparison.Ordinal);
        Assert.Contains("  Cart ---|\"Partnership\"| Catalog", md, StringComparison.Ordinal);
    }

    [Fact]
    public void OptionsControlSections()
    {
        var md = ContextMapRenderer.Of(Arch)
            .WithMermaid(false)
            .IncludeExternalSystems(false)
            .IncludePlanned(false)
            .WithTitle("Strategic Map")
            .Render();
        Assert.StartsWith("# Strategic Map\n", md, StringComparison.Ordinal);
        Assert.DoesNotContain("```mermaid", md, StringComparison.Ordinal);
        Assert.DoesNotContain("## External systems", md, StringComparison.Ordinal);
        Assert.DoesNotContain("Carrier API", md, StringComparison.Ordinal);
        Assert.DoesNotContain("planned", md, StringComparison.Ordinal);
        Assert.Contains("## Upstream relationships", md, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteToCreatesParentDirectoriesAndOverwrites()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dca-contextmap-" + Guid.NewGuid().ToString("N"));
        try
        {
            var target = Path.Combine(dir, "docs", "architecture", "context-map.md");
            ContextMapRenderer.Of(Arch).WriteTo(target);
            Assert.True(File.Exists(target));
            Assert.Equal(ContextMapRenderer.Of(Arch).Render(), File.ReadAllText(target));

            ContextMapRenderer.Of(Arch).WithTitle("Other").WriteTo(target);
            Assert.StartsWith("# Other", File.ReadAllText(target), StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }
}
