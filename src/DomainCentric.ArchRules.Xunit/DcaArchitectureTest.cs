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
/// <para>
/// Experimental rules (<see cref="DcaRuleMaturity.Experimental"/>) form a group of their own,
/// <c>experimental (1) / DCA-USE-0nn</c> with the number of experimental rules in scope, after the rule sets, and the summary counts them apart from the
/// released rules. They run only where the project opted in — <c>dca.rules.experimental = true</c> in the
/// file, or <see cref="DcaRuleSelection.WithExperimental"/> in <see cref="AdditionalSelection"/>. Without
/// the file's opt-in their cases carry <c>[NOT OPTED IN]</c> and pass without running, unless
/// <see cref="AdditionalSelection"/> opted in.
/// </para>
/// </example>
public abstract class DcaArchitectureTest
{
    private static readonly object Gate = new();
    private static readonly Dictionary<Type, DcaArchitecture> Cache = new();
    private static readonly IReadOnlyList<IDcaRuleSet> CatalogSets = DcaRules.RuleSets(DcaLayout.ForRootNamespace("Catalog"));

    /// <summary>
    /// The report group of the experimental rules, beside the rule sets; a case name carries it with the
    /// number of experimental rules in scope, <c>experimental (2)</c>.
    /// </summary>
    public const string ExperimentalGroup = "experimental";

    /// <summary>The layout of the project under test.</summary>
    protected abstract DcaLayout Layout { get; }

    /// <summary>The production assemblies to import (one or more).</summary>
    protected abstract IEnumerable<System.Reflection.Assembly> Assemblies { get; }

    /// <summary>
    /// <see cref="Layout"/> with <c>dca.framework=&lt;name&gt;</c> from <c>dca-archunit.properties</c>
    /// applied — unless the layout names its framework types in code, which always wins. The same key
    /// and the same precedence as in <c>dca-archunit</c>.
    /// </summary>
    protected DcaLayout EffectiveLayout => DcaLayout.ApplyConfiguredFramework(Layout, FileProperties);

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
    protected virtual IReadOnlyList<IDcaRule> Rules => DcaRules.SelectFlat(EffectiveLayout, Selection);

    /// <summary>The imported architecture; loaded once per test class.</summary>
    protected DcaArchitecture Architecture
    {
        get
        {
            lock (Gate)
            {
                if (!Cache.TryGetValue(GetType(), out var arch))
                {
                    arch = DcaArchitecture.Load(EffectiveLayout, Assemblies.ToArray());
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

    /// <summary>The raw key/value pairs of the same file, for the settings outside the rule selection.</summary>
    private static readonly IReadOnlyDictionary<string, string> FileProperties =
        DcaRuleSelection.PropertiesFrom(AppContext.BaseDirectory);

    /// <summary>
    /// The rules the properties file selects, prefixed by their set and annotated with their
    /// severity. A rule scoped out by <c>dca.rules.sets</c> / <c>dca.rules.ids</c> produces no theory
    /// case at all, so the test count says what was actually checked.
    /// </summary>
    public static IEnumerable<object[]> RuleIds() =>
        CaseNames(CatalogSets, FileSelection).Select(name => new object[] { name });

    /// <summary>
    /// The theory case names for the rules of <paramref name="ruleSets"/> under
    /// <paramref name="selection"/>: <c>&lt;set&gt; / &lt;id&gt;</c> for every released rule in scope, in
    /// catalog order, then <c>experimental (n) / &lt;id&gt;</c> for every experimental rule in scope, n being
    /// their number. Each name
    /// carries the rule's lowered severity (<c>[WARN: …]</c>, <c>[OFF: …]</c>); an experimental rule of a
    /// selection that did not opt in carries <c>[NOT OPTED IN]</c> instead.
    /// </summary>
    public static IReadOnlyList<string> CaseNames(IEnumerable<IDcaRuleSet> ruleSets, DcaRuleSelection selection)
    {
        ArgumentNullException.ThrowIfNull(ruleSets);
        ArgumentNullException.ThrowIfNull(selection);
        var released = new List<string>();
        var experimental = new List<(string Id, string Annotation)>();
        foreach (var set in ruleSets)
        {
            foreach (var rule in set.Rules.Where(r => selection.Includes(set.Name, r.Id)))
            {
                if (rule.Maturity == DcaRuleMaturity.Experimental)
                {
                    var annotation = selection.IncludesExperimental ? Annotation(selection, rule.Id) : " [NOT OPTED IN]";
                    experimental.Add((rule.Id, annotation));
                }
                else
                {
                    released.Add($"{set.Name} / {rule.Id}{Annotation(selection, rule.Id)}");
                }
            }
        }

        var group = $"{ExperimentalGroup} ({experimental.Count})";
        return released.Concat(experimental.Select(e => $"{group} / {e.Id}{e.Annotation}")).ToList();
    }

    /// <summary>
    /// How many of <paramref name="rules"/> are released and enforced, released and informational, and
    /// experimental — <c>40 enforced, 3 informational, 1 experimental</c>. An experimental rule counts
    /// only as experimental, whatever its kind.
    /// </summary>
    public static string Summary(IEnumerable<IDcaRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var list = rules.ToList();
        var experimental = list.Count(r => r.Maturity == DcaRuleMaturity.Experimental);
        var informational = list.Count(r => r.Maturity == DcaRuleMaturity.Released && r.Kind == DcaRuleKind.Informational);
        var enforced = list.Count - experimental - informational;
        return $"{enforced} enforced, {informational} informational, {experimental} experimental";
    }

    private static string Annotation(DcaRuleSelection selection, string ruleId)
    {
        var severity = selection.SeverityOf(ruleId);
        if (severity == DcaSeverity.Error)
        {
            return string.Empty;
        }

        var reason = selection.ReasonFor(ruleId);
        var label = severity == DcaSeverity.Warn ? "WARN" : "OFF";
        return reason is null ? $" [{label}]" : $" [{label}: {reason}]";
    }

    /// <summary>
    /// The two facts of the layout worth seeing in the report: the framework preset the rules
    /// resolved their attributes with, and the vocabulary they resolved their building blocks with.
    /// The wording matches the <c>layout</c> container of the Java carrier, so a run is
    /// self-describing on both stacks.
    /// </summary>
    [Fact]
    public void LayoutDiagnostics() {
        var layout = EffectiveLayout;
        Console.Out.WriteLine($"framework annotations: {layout.FrameworkTypesReport()}");
        Console.Out.WriteLine($"building block markers: {layout.MarkersReport()}");
    }

    [Fact]
    public void CatalogKindsAndRetiredIdentities() {
        Console.Out.WriteLine(Summary(Rules));
        foreach (var id in Selection.RetiredReferences) {
            var retired = DcaRules.Retired()[id];
            Console.Out.WriteLine($"{id} is retired since {retired.Since}: {retired.Reason}; replacement: {retired.Replacement}");
        }
    }

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void DcaRule(string qualifiedRuleId)
    {
        var ruleId = RuleIdOf(qualifiedRuleId);
        var rule = Rules.FirstOrDefault(r => r.Id == ruleId);
        if (rule is null)
        {
            // Not opted in, or scoped out by AdditionalSelection, which cannot shape the static theory data.
            var experimental = CatalogSets.SelectMany(s => s.Rules)
                .Any(r => r.Id == ruleId && r.Maturity == DcaRuleMaturity.Experimental);
            Console.Out.WriteLine(experimental && !Selection.IncludesExperimental
                ? $"[{ruleId}] not run — experimental; opt in with {DcaRuleSelection.ExperimentalKey} = true or WithExperimental()"
                : $"[{ruleId}] not run — outside the configured rule selection");
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
                // A diagnostic that observed something passes and carries its lines; report them the
                // way a warning is reported. xUnit v2 cannot turn a running test into a skipped one,
                // so this stays on the test output — but the lines are in the outcome, where a report
                // or a pipeline stage can read them. The Java twin aborts the test instead.
                if (outcome.Message is not null)
                {
                    Console.Out.WriteLine($"[{ruleId}] INFO — {outcome.Message}");
                }

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
