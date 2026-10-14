using System.Linq;
using Xunit;
namespace DomainCentric.ArchRules.Tests.Rules;

public class EventPolicyTests
{
    private const string Root = "DomainCentric.ArchRules.Tests.Fixtures.EventPolicy";
    private static DcaArchitecture Arch() => DcaArchitecture.Load(DcaLayout.ForRootNamespace(Root), typeof(EventPolicyTests).Assembly);
    private static IDcaRule Rule(DcaArchitecture arch, string id) => DcaRules.All(arch.Layout).Single(r => r.Id == id);
    [Fact]
    public void EventFreeSavePassesAndRecordedSaveFails()
    {
        var arch = Arch(); var message = Assert.Throws<DcaRuleViolationException>(() => Rule(arch, "DCA-USE-009").Check(arch)).Message;
        Assert.Contains("Recording.SaveUseCase", message); Assert.Contains("Unresolved.SaveUseCase", message); Assert.DoesNotContain("Free.SaveUseCase", message);
        // A nested helper registers events on Note. The library's RegisterEvent is protected, so only a
        // nested type can reach it - and a nested type belongs to the enclosing type's call graph, so the
        // hierarchy walk already sees this. The external scan covers the same shape for an own vocabulary.
        Assert.Contains("External.SaveUseCase", message);
    }
    [Fact]
    public void BoundaryCoverageHasDocumentedStaticLimit()
    {
        var layout = DcaLayout.ForRootNamespace(Root).WithFrameworkTypes(FrameworkTypes.AspNetCore() with { TransactionalAttribute = Root + ".Module.Application.Declarative.WorkAttribute" });
        var arch = DcaArchitecture.Load(layout, typeof(EventPolicyTests).Assembly); var message = Assert.Throws<DcaRuleViolationException>(() => Rule(arch, "DCA-USE-012").Check(arch)).Message;
        Assert.DoesNotContain("AttributedUseCase", message);
        Assert.Contains("UncoveredUseCase", message); Assert.DoesNotContain("AfterEmptyUseCase", message); Assert.DoesNotContain("CoveredUseCase", message);
    }
    [Fact]
    public void EventFreeSaveStillNeedsATransactionBoundary()
    {
        // DCA-USE-009 exempts the event-free save from publishing; DCA-USE-012 still wants the unit of work drawn.
        var arch = Arch(); var message = Assert.Throws<DcaRuleViolationException>(() => Rule(arch, "DCA-USE-012").Check(arch)).Message;
        Assert.Contains("Free.SaveUseCase.ExecuteAsync saves an aggregate without", message);
        Assert.Contains("Free.DeleteUseCase.ExecuteAsync deletes an aggregate without", message);
        Assert.DoesNotContain("BoundedSaveUseCase", message); Assert.DoesNotContain("AfterEmptyUseCase", message);
    }
    [Fact]
    public void IntegrationEventPlacementFollowsTheConfiguredEventsSegment()
    {
        // With the default layout the contract in Module.Events passes; renaming the Events segment makes the same
        // type misplaced - the segment is read from the layout, not hard-coded.
        var arch = Arch(); Assert.DoesNotContain("Exported", Assert.ThrowsAny<System.Exception>(() => Rule(arch, "DCA-STR-007").Check(arch)).Message);
        var renamed = DcaArchitecture.Load(DcaLayout.ForRootNamespace(Root).WithEventsSegment("Contracts"), typeof(EventPolicyTests).Assembly);
        Assert.Contains("Exported", Assert.ThrowsAny<System.Exception>(() => Rule(renamed, "DCA-STR-007").Check(renamed)).Message);
    }
    [Fact]
    public void BusinessVersionIsAllowedButAdapterContractsAreNot()
    {
        var arch = Arch(); Rule(arch, "DCA-ADV-006").Check(arch); Rule(arch, "DCA-ADV-007").Check(arch);
        Assert.Contains("Misplaced", Assert.ThrowsAny<System.Exception>(() => Rule(arch, "DCA-STR-007").Check(arch)).Message);
    }
    [Fact]
    public void AnExternalRegistrationOnAnOwnVocabularyDisablesTheExemption()
    {
        // The library's own RegisterEvent is protected, so only a nested type can reach it - and a nested
        // type already belongs to the enclosing type's call graph. A project whose own aggregate base
        // exposes the registration is the case the external scan exists for; Java has it since 0.4.0.
        const string root = "DomainCentric.ArchRules.Tests.Fixtures.OwnEvents";
        var layout = DcaLayout.ForRootNamespace(root).WithMarkers(DcaMarkers.Dca() with
        {
            AggregateRoot = root + ".Vocabulary.OwnAggregateBase"
        });
        var arch = DcaArchitecture.Load(layout, typeof(EventPolicyTests).Assembly);
        var message = Assert.Throws<DcaRuleViolationException>(() => DcaRules.All(layout).Single(r => r.Id == "DCA-USE-009").Check(arch)).Message;
        Assert.Contains("SaveTicketUseCase", message);
    }
}
