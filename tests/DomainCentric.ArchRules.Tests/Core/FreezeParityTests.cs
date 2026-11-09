using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Core;

/// <summary>
/// <c>dca.rules.freeze</c> means the same in both libraries, for the same rules: an id is freezable here
/// exactly when it is freezable in the Java library. Read from the Java library's <c>rules.json</c> where a
/// checkout lies beside this one; without it there is nothing to compare and the test passes.
/// </summary>
public sealed class FreezeParityTests
{
    [Fact]
    public void AnIdIsFreezableHereExactlyWhenItIsFreezableInJava()
    {
        var javaCatalog = JavaRulesJson();
        if (javaCatalog is null)
        {
            return;
        }

        using var json = JsonDocument.Parse(File.ReadAllText(javaCatalog));
        var java = json.RootElement.GetProperty("rules").EnumerateArray()
            .Where(rule => rule.TryGetProperty("freezable", out _))
            .ToDictionary(rule => rule.GetProperty("id").GetString()!, rule => rule.GetProperty("freezable").GetBoolean(), StringComparer.Ordinal);
        Assert.NotEmpty(java);

        var mismatches = DcaRules.All(DcaLayout.ForRootNamespace("Catalog"))
            .Where(rule => java.ContainsKey(rule.Id) && java[rule.Id] != rule.Freezable)
            .Select(rule => $"{rule.Id}: freezable in {(rule.Freezable ? ".NET" : "Java")} only")
            .ToList();

        Assert.True(mismatches.Count == 0, "Freezable in one language only:\n" + string.Join("\n", mismatches));
    }

    private static string? JavaRulesJson()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "dca-dotnet.sln")))
            {
                var candidate = Path.Combine(directory.FullName, "..", "dca-java", "rules.json");
                return File.Exists(candidate) ? candidate : null;
            }
        }

        return null;
    }
}
