using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Core;

/// <summary>
/// Isolation is structural. The isolation rules (<c>DCA-STR-003</c>, <c>DCA-STR-004</c>, <c>DCA-STR-006</c>,
/// <c>DCA-HEX-007</c>) select over <see cref="DcaArchitecture.IsolatedModuleRoots"/> — every module that owns
/// a DCA layer — and not over the declared bounded contexts. A module that declares no <c>[BoundedContext]</c>
/// is therefore governed as a source and protected as a target exactly like a declared one; the declaration
/// decides context-map membership and nothing else.
/// </summary>
public sealed class StructuralIsolationTests
{
    private const string Root = "DomainCentric.ArchRules.Tests.Fixtures.Layout.Isolation";
    private const string Internals = Root + ".Catalog.Domain.Model";

    private static DcaArchitecture Arch(DcaLayout? layout = null) =>
        DcaArchitecture.Load(layout ?? DcaLayout.ForRootNamespace(Root), typeof(StructuralIsolationTests).Assembly);

    /// <summary>Every failure of the full catalog, as "id :: message".</summary>
    private static List<string> Failures(DcaArchitecture arch)
    {
        var messages = new List<string>();
        foreach (var rule in DcaRules.All(arch.Layout))
        {
            try
            {
                rule.Check(arch);
            }
            catch (DcaRuleViolationException e)
            {
                messages.Add(rule.Id + " :: " + e.Message.Replace('\n', ' '));
            }
        }

        return messages;
    }

    private static List<string> FailuresOf(string ruleId) =>
        Failures(Arch()).Where(m => m.StartsWith(ruleId + " ", System.StringComparison.Ordinal)).ToList();

    [Fact]
    public void TheFixtureDiffersInTheDeclarationAndInNothingElse()
    {
        var arch = Arch();
        Assert.Superset(
            new HashSet<string> { Root + ".Catalog", Root + ".Peer", Root + ".Reporting" },
            new HashSet<string>(arch.IsolatedModuleRoots()));
        Assert.Contains(Root + ".Peer", arch.BoundedContexts.Keys);
        Assert.DoesNotContain(Root + ".Reporting", arch.BoundedContexts.Keys);
    }

    [Fact]
    public void ADeclaredContextIsCaughtImportingAnotherModulesInternals() =>
        Assert.Contains(FailuresOf("DCA-STR-003"), m => m.Contains("GetPeerUseCase") && m.Contains(Internals));

    [Fact]
    public void AnUndeclaredModuleIsCaughtDoingTheSameThing() =>
        Assert.Contains(FailuresOf("DCA-STR-003"), m => m.Contains("GetReportUseCase") && m.Contains(Internals));

    [Fact]
    public void AnUndeclaredModuleIsProtectedAsATarget() =>
        Assert.Contains(FailuresOf("DCA-STR-003"), m => m.Contains("DescribeProductUseCase") && m.Contains(Root + ".Reporting.Application"));

    [Fact]
    public void AnUndeclaredModulesIncomingAdapterMustStayInItsOwnModule() =>
        Assert.Contains(FailuresOf("DCA-HEX-007"), m => m.Contains("ReportController"));

    [Fact]
    public void TheDomainLayerMayNotDependOnAnotherModuleNotEvenItsApi() =>
        Assert.Contains(FailuresOf("DCA-STR-004"), m => m.Contains("PeerListing"));

    [Fact]
    public void AnOutgoingAdapterMayUseAForeignApiButNotForeignInternals()
    {
        var str006 = FailuresOf("DCA-STR-006");
        Assert.Contains(str006, m => m.Contains("CatalogInternalsClient"));
        Assert.DoesNotContain(str006, m => m.Contains("Catalog.CatalogClient"));
    }

    /// <summary>
    /// The allow-list is a layout setting, like every other namespace segment. Renaming the published
    /// segment makes the former Api namespace internal — the same adapter that passed is reported.
    /// </summary>
    [Fact]
    public void ThePublishedSegmentsComeFromTheLayout()
    {
        var layout = DcaLayout.ForRootNamespace(Root).WithApiSegment("Contract");
        var arch = Arch(layout);
        Assert.Contains(DcaLayout.Below(Root + ".Catalog.Contract"), arch.PublishedPatternsExcluding(Root + ".Peer"));
        var str006 = DcaRules.All(layout).Single(r => r.Id == "DCA-STR-006");
        var ex = Assert.Throws<DcaRuleViolationException>(() => str006.Check(arch));
        Assert.Contains("Catalog.CatalogClient", ex.Message, System.StringComparison.Ordinal);
    }

    [Fact]
    public void TheLayoutRejectsIdenticalPublishedSegments() =>
        Assert.Throws<System.ArgumentException>(() => DcaLayout.ForRootNamespace(Root).WithApiSegment("Events"));

    [Fact]
    public void NoRuleAsksAModuleToDeclareItself()
    {
        Assert.DoesNotContain(DcaRules.All(DcaLayout.ForRootNamespace(Root)), r => r.Id == "DCA-LAY-006");
        Assert.DoesNotContain(Failures(Arch()), m => m.Contains("declares nothing"));
    }
}
