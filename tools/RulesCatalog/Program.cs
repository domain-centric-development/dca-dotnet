// Renders the DCA rule catalog of DomainCentric.ArchRules to rules.json and RULES.md — the same
// shape dca-archunit's `rulesCatalog` Gradle task produces (id, title, rationale, selects, checks), so the
// knowledge catalog can merge both.
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
        entries.Add(new Entry(set.Name, rule.Id, rule.Title, rule.Rationale, rule.Selects, rule.Checks, rule.Kind == DcaRuleKind.Informational ? "informational" : "enforced", null));
    }

    foreach (var (id, reason) in NotApplicable(set))
    {
        entries.Add(new Entry(set.Name, id, "(not applicable in .NET)", reason, "", "", "n/a", reason));
    }
}

entries = entries.OrderBy(e => ruleSets.ToList().FindIndex(s => s.Name == e.Set)).ThenBy(e => e.Id, StringComparer.Ordinal).ToList();

var json = JsonSerializer.Serialize(
    new { rules = entries.Select(e => e.Status != "n/a"
        ? (object)new { set = e.Set, id = e.Id, title = e.Title, rationale = e.Rationale, selects = e.Selects, checks = e.Checks, implementation = "dotnet", status = e.Status }
        : new { set = e.Set, id = e.Id, status = "n/a", reason = e.Reason }), retired = DcaRules.Retired().OrderBy(e => e.Key).Select(e => new { id = e.Key, reason = e.Value.Reason, replacement = e.Value.Replacement, since = e.Value.Since }) },
    new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
File.WriteAllText(Path.Combine(root, "rules.json"), json + "\n");

var md = new StringBuilder();
var ported = entries.Count(e => e.Status != "n/a");
var informational = entries.Count(e => e.Status == "informational");
var enforced = ported - informational;
var na = entries.Count(e => e.Status == "n/a");
md.Append("# DCA rule catalog (.NET)\n\n");
md.Append($"Generated from `DomainCentric.ArchRules` — do not edit. {enforced} enforced, {informational} informational, {DcaRules.Retired().Count} retired in {ruleSets.Count} sets; {na} Java rules not applicable in .NET.\n\n");
foreach (var set in ruleSets)
{
    md.Append($"## `{set.Name}`\n\n| Id | Rule | Rationale | Selects | Checks |\n|----|------|-----------|---------|--------|\n");
    foreach (var e in entries.Where(e => e.Set == set.Name && e.Status != "n/a"))
    {
        md.Append($"| `{e.Id}` | {Cell(e.Title)} ({e.Status}) | {Cell(e.Rationale)} | {Cell(e.Selects)} | {Cell(e.Checks)} |\n");
    }

    var skipped = entries.Where(e => e.Set == set.Name && e.Status == "n/a").ToList();
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

md.Append("## Retired identities\n\n");
foreach (var (id, value) in DcaRules.Retired().OrderBy(e => e.Key))
    md.Append($"- `{id}` — {value.Reason}; replacement: {value.Replacement}; since {value.Since}\n");
File.WriteAllText(Path.Combine(root, "RULES.md"), md.ToString());
Console.WriteLine($"{enforced} enforced, {informational} informational, {DcaRules.Retired().Count} retired, {na} n/a → rules.json, RULES.md");

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

sealed record Entry(string Set, string Id, string Title, string Rationale, string Selects, string Checks, string Status, string? Reason);
