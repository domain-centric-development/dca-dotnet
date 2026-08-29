// Renders the DCA rule catalog of DomainCentric.ArchRules to rules.json and RULES.md — the same
// shape dca-archunit's `rulesCatalog` Gradle task produces, so the knowledge catalog can merge both.
//
//   dotnet run --project tools/RulesCatalog -- <repo-root>
using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DomainCentric.ArchRules;

var root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
var layout = DcaLayout.ForRootNamespace("Catalog");
var ruleSets = DcaRules.RuleSets(layout);

var entries = new List<Entry>();
foreach (var set in ruleSets)
{
    foreach (var rule in set.Rules)
    {
        entries.Add(new Entry(set.Name, rule.Id, rule.Title, rule.Rationale, "ported", null));
    }

    foreach (var (id, reason) in NotApplicable(set))
    {
        entries.Add(new Entry(set.Name, id, "(not applicable in .NET)", reason, "n/a", reason));
    }
}

entries = entries.OrderBy(e => ruleSets.ToList().FindIndex(s => s.Name == e.Set)).ThenBy(e => e.Id, StringComparer.Ordinal).ToList();

var json = JsonSerializer.Serialize(
    entries.Select(e => e.Status == "ported"
        ? (object)new { set = e.Set, id = e.Id, title = e.Title, rationale = e.Rationale, implementation = "dotnet" }
        : new { set = e.Set, id = e.Id, status = "n/a", reason = e.Reason }),
    new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
File.WriteAllText(Path.Combine(root, "rules.json"), json + "\n");

var md = new StringBuilder();
var ported = entries.Count(e => e.Status == "ported");
var na = entries.Count(e => e.Status != "ported");
md.Append("# DCA rule catalog (.NET)\n\n");
md.Append($"Generated from `DomainCentric.ArchRules` — do not edit. {ported} rules in {ruleSets.Count} sets; {na} Java rules not applicable in .NET.\n\n");
foreach (var set in ruleSets)
{
    md.Append($"## `{set.Name}`\n\n| Id | Rule | Rationale |\n|----|------|-----------|\n");
    foreach (var e in entries.Where(e => e.Set == set.Name && e.Status == "ported"))
    {
        md.Append($"| `{e.Id}` | {Cell(e.Title)} | {Cell(e.Rationale)} |\n");
    }

    var skipped = entries.Where(e => e.Set == set.Name && e.Status != "ported").ToList();
    if (skipped.Count > 0)
    {
        md.Append("\nNot applicable in .NET:\n\n");
        foreach (var e in skipped)
        {
            md.Append($"- `{e.Id}` — {e.Reason}\n");
        }
    }

    md.Append('\n');
}

File.WriteAllText(Path.Combine(root, "RULES.md"), md.ToString());
Console.WriteLine($"{ported} rules, {na} n/a → rules.json, RULES.md");

static string Cell(string s) => s.Replace("|", "\\|").Replace("\n", " ");

static IEnumerable<(string, string)> NotApplicable(IDcaRuleSet set)
{
    var field = set.GetType().GetField("NotApplicable", BindingFlags.Public | BindingFlags.Static);
    if (field?.GetValue(null) is IEnumerable dict)
    {
        foreach (var item in dict)
        {
            var key = item.GetType().GetProperty("Key")!.GetValue(item)!.ToString()!;
            var value = item.GetType().GetProperty("Value")!.GetValue(item)!.ToString()!;
            yield return (key, value);
        }
    }
}

sealed record Entry(string Set, string Id, string Title, string Rationale, string Status, string? Reason);
