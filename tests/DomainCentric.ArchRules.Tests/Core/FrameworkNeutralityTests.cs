using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Core;

/// <summary>
/// The rules and the building blocks are the foundation any production system builds on — any industry,
/// any framework. Their texts flow verbatim into the knowledge catalog, so they must not speak Spring or
/// ASP.NET, and they must not speak shop. A framework or shop word in a rule's title, rationale,
/// <c>Selects</c> or <c>Checks</c>, in a not-applicable reason, or in a building block's XML-doc prose
/// fails the build — unless the sentence marks it as an example ("for example", "e.g.", "such as").
/// The Java twin runs the same guard.
/// </summary>
public sealed class FrameworkNeutralityTests
{
    private static readonly Regex Framework = new(
        @"\b(Spring|Modulith|JPA|Jakarta|Hibernate|Quarkus|Micronaut|MediatR|EF Core|EntityFramework|AutoMapper)\b"
        + @"|@(Service|Component|Transactional\w*|ApplicationModule\w*|Controller|RestController|EventListener|Entity)\b",
        RegexOptions.Compiled);

    /// <summary>Shop vocabulary. <c>Order</c> only capitalised — the lower-case word is ordinary English.</summary>
    private static readonly Regex Shop = new(
        @"(?i)\b(carts?|checkout|products?|inventory|pricing|customers?|shop)\b|\bOrders?\b",
        RegexOptions.Compiled);

    private static readonly Regex ExampleMarker = new(
        @"for\s+example|e\.g\.|such\s+as|one\s+implementation|for\s+instance",
        RegexOptions.Compiled);

    private static readonly Regex Code = new(
        @"<code>.*?</code>|<c>.*?</c>|<see\s[^>]*/>|<seealso\s[^>]*/>",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex DddTerms = new(@"Customer[–/-]Supplier", RegexOptions.Compiled);

    [Fact]
    public void RuleTextsAreNeutral()
    {
        var offences = new List<string>();
        foreach (var set in DcaRules.RuleSets(DcaLayout.ForRootNamespace("Example")))
        {
            foreach (var rule in set.Rules)
            {
                foreach (var text in new[] { rule.Title, rule.Rationale, rule.Selects, rule.Checks })
                {
                    offences.AddRange(OffencesIn(rule.Id, text));
                }
            }

            foreach (var (id, reason) in NotApplicable(set))
            {
                offences.AddRange(OffencesIn(id + " (n/a reason)", reason));
            }
        }

        Assert.True(offences.Count == 0, "\n" + string.Join("\n", offences));
    }

    [Fact]
    public void BuildingBlockDocsAreNeutral()
    {
        var sources = Path.Combine(RepoRoot(), "src", "DomainCentric.BuildingBlocks");
        Assert.True(Directory.Exists(sources), "expected the building blocks at " + sources);
        var offences = new List<string>();
        foreach (var file in Directory.EnumerateFiles(sources, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            offences.AddRange(OffencesIn(Path.GetFileName(file), XmlDocProse(File.ReadAllText(file))));
        }

        Assert.True(offences.Count == 0, "\n" + string.Join("\n", offences));
    }

    private static IEnumerable<string> OffencesIn(string where, string text)
    {
        var prose = DddTerms.Replace(Code.Replace(text, " "), " ");
        foreach (var sentence in Regex.Split(prose, @"(?<=[.;])\s+"))
        {
            if (ExampleMarker.IsMatch(sentence))
            {
                continue;
            }

            var framework = Framework.Match(sentence);
            if (framework.Success)
            {
                yield return $"{where}: framework word '{framework.Value}' in: {sentence.Trim()}";
                continue;
            }

            var shop = Shop.Match(sentence);
            if (shop.Success)
            {
                yield return $"{where}: shop word '{shop.Value}' in: {sentence.Trim()}";
            }
        }
    }

    /// <summary>The <c>///</c> comment lines of a source file, markers removed, joined into prose.</summary>
    private static string XmlDocProse(string source) =>
        string.Join(
            "\n",
            source.Split('\n')
                .Select(l => l.TrimStart())
                .Where(l => l.StartsWith("///", StringComparison.Ordinal))
                .Select(l => l.Substring(3).Trim()));

    private static IEnumerable<(string, string)> NotApplicable(IDcaRuleSet set)
    {
        var field = set.GetType().GetField("NotApplicable");
        if (field?.GetValue(null) is IReadOnlyDictionary<string, string> dict)
        {
            foreach (var pair in dict)
            {
                yield return (pair.Key, pair.Value);
            }
        }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "dca-dotnet.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("dca-dotnet.sln not found above " + AppContext.BaseDirectory);
    }
}
