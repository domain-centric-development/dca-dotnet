using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace DomainCentric.ArchRules.Xunit;

/// <summary>
/// xUnit base class that runs the whole DCA rule catalog as one theory case per rule, named by set and
/// id: <c>tactical / DCA-TAC-001</c>, <c>tactical / DCA-TAC-002</c>, …
/// </summary>
/// <example>
/// <code>
/// public sealed class ArchitectureTest : DcaArchitectureTest
/// {
///     protected override DcaLayout Layout => DcaLayout.ForRootNamespace("Acme.Shop");
///
///     protected override IEnumerable&lt;Assembly&gt; Assemblies => new[] { typeof(Program).Assembly };
/// }
/// </code>
/// Unless <see cref="Selection"/> is overridden, the configuration comes from
/// <c>dca-archunit.properties</c> next to the test assembly — see
/// <see cref="DcaRuleSelection.FromDirectory"/> — plus whatever <see cref="AdditionalSelection"/>
/// adds on top. A rule the *file* lowers to <see cref="DcaSeverity.Warn"/> or switches off carries its
/// severity and recorded reason in the test's display name
/// (<c>naming / DCA-NAM-005 [OFF: no MVC controllers]</c>) and prints its violations to the test
/// output; a rule the file scopes out produces no theory case at all. Settings made in
/// <see cref="AdditionalSelection"/> take effect but cannot change the display name, because theory
/// data is built before an instance exists. xUnit v2 has no dynamic skip, which is why a lowered rule
/// is reported green rather than skipped; the Java twin aborts it.
/// </example>
public abstract class DcaArchitectureTest
{
    private static readonly object Gate = new();
    private static readonly Dictionary<Type, DcaArchitecture> Cache = new();
    private static readonly IReadOnlyDictionary<string, string> SetOfRule = DcaRules.SetOfRule();

    /// <summary>The layout of the project under test.</summary>
    protected abstract DcaLayout Layout { get; }

    /// <summary>The production assemblies to import (one or more).</summary>
    protected abstract IEnumerable<System.Reflection.Assembly> Assemblies { get; }

    /// <summary>
    /// Which rules run and how strictly: <c>dca-archunit.properties</c> next to the test assembly,
    /// with <see cref="AdditionalSelection"/> applied on top.
    /// </summary>
    /// <remarks>
    /// Override <see cref="AdditionalSelection"/> to configure rules in code — overriding this
    /// property replaces the properties file instead of adding to it.
    /// </remarks>
    protected virtual DcaRuleSelection Selection =>
        FileSelection.MergedWith(FromLegacyOverrides()).MergedWith(AdditionalSelection);

    /// <summary>
    /// Project-specific configuration applied on top of <c>dca-archunit.properties</c>. Default:
    /// none, so the file alone decides.
    /// </summary>
    /// <remarks>
    /// Only the file-based configuration can shape the test list: theory cases are built statically,
    /// before an instance exists. A rule the file scopes out or lowers is therefore named as such
    /// (<c>naming / DCA-NAM-005 [OFF: …]</c>), while the same setting made here shows up only in the
    /// test output. Prefer the file when the report should carry the decision.
    /// </remarks>
    protected virtual DcaRuleSelection AdditionalSelection => DcaRuleSelection.All();

    /// <summary>Identifiers of rules to skip. Default: none. Prefer <see cref="Selection"/>.</summary>
    protected virtual ISet<string> ExcludedRuleIds => new HashSet<string>();

    /// <summary>The rules to run. Default: whatever <see cref="Selection"/> asks for.</summary>
    protected virtual IReadOnlyList<IDcaRule> Rules => DcaRules.SelectFlat(Layout, Selection);

    /// <summary>The imported architecture; loaded once per test class.</summary>
    protected DcaArchitecture Architecture
    {
        get
        {
            lock (Gate)
            {
                if (!Cache.TryGetValue(GetType(), out var arch))
                {
                    arch = DcaArchitecture.Load(Layout, Assemblies.ToArray());
                    Cache[GetType()] = arch;
                }

                return arch;
            }
        }
    }

    /// <summary>
    /// The configuration found next to the test assembly. Read statically so that a lowered rule can
    /// be named as such in the theory's display name.
    /// </summary>
    private static readonly DcaRuleSelection FileSelection = DcaRuleSelection.FromDirectory(AppContext.BaseDirectory);

    /// <summary>
    /// The rules the properties file selects, prefixed by their set and annotated with their
    /// severity. A rule scoped out by <c>dca.rules.sets</c> / <c>dca.rules.ids</c> produces no theory
    /// case at all, so the test count says what was actually checked.
    /// </summary>
    public static IEnumerable<object[]> RuleIds() =>
        DcaRules.AllIds()
            .Where(id => FileSelection.Includes(SetOfRule[id], id))
            .Select(id => new object[] { $"{SetOfRule[id]} / {id}{Annotation(id)}" });

    private static string Annotation(string ruleId)
    {
        var severity = FileSelection.SeverityOf(ruleId);
        if (severity == DcaSeverity.Error)
        {
            return string.Empty;
        }

        var reason = FileSelection.ReasonFor(ruleId);
        var label = severity == DcaSeverity.Warn ? "WARN" : "OFF";
        return reason is null ? $" [{label}]" : $" [{label}: {reason}]";
    }

    [Fact]
    public void CatalogKindsAndRetiredIdentities() {
        var selected = Rules.ToList();
        var informational = selected.Count(r => r.Kind == DcaRuleKind.Informational);
        Console.Out.WriteLine($"{selected.Count - informational} enforced, {informational} informational; retired: {string.Join(", ", DcaRules.Retired().Keys)}");
    }

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void DcaRule(string qualifiedRuleId)
    {
        var ruleId = RuleIdOf(qualifiedRuleId);
        var rule = Rules.FirstOrDefault(r => r.Id == ruleId);
        if (rule is null)
        {
            // Scoped out by AdditionalSelection, which cannot shape the static theory data.
            Console.Out.WriteLine($"[{ruleId}] not run — outside the configured rule selection");
            return;
        }

        var outcome = DcaRuleExecution.Execute(rule, Architecture, Selection);
        switch (outcome.Status)
        {
            case DcaRuleStatus.Failed:
                throw new DcaRuleViolationException(outcome.Message!);
            case DcaRuleStatus.Warned:
                Console.Out.WriteLine($"[{ruleId}] WARNING — {outcome.Message}");
                break;
            case DcaRuleStatus.Skipped:
                Console.Out.WriteLine($"[{ruleId}] switched off — {outcome.Message}");
                break;
            default:
                break;
        }
    }

    /// <summary>Extracts the bare rule id from the annotated display name.</summary>
    private static string RuleIdOf(string qualifiedRuleId)
    {
        var withoutSet = qualifiedRuleId[(qualifiedRuleId.IndexOf('/') + 1)..].Trim();
        var annotation = withoutSet.IndexOf(" [", StringComparison.Ordinal);
        return annotation < 0 ? withoutSet : withoutSet[..annotation];
    }

    /// <summary>Bridges the older <see cref="ExcludedRuleIds"/> hook onto the selection.</summary>
    private DcaRuleSelection FromLegacyOverrides()
    {
        var selection = DcaRuleSelection.All();
        foreach (var id in ExcludedRuleIds)
        {
            selection = selection.Excluding(id);
        }

        return selection;
    }
}
