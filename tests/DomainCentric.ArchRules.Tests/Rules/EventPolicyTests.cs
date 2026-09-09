using System.Linq;
using Xunit;
namespace DomainCentric.ArchRules.Tests.Rules;
public class EventPolicyTests {
 private const string Root="DomainCentric.ArchRules.Tests.Fixtures.EventPolicy";
 private static DcaArchitecture Arch()=>DcaArchitecture.Load(DcaLayout.ForRootNamespace(Root),typeof(EventPolicyTests).Assembly);
 private static IDcaRule Rule(DcaArchitecture arch,string id)=>DcaRules.All(arch.Layout).Single(r=>r.Id==id);
 [Fact] public void EventFreeSavePassesAndRecordedSaveFails() {
  var arch=Arch();var message=Assert.Throws<DcaRuleViolationException>(()=>Rule(arch,"DCA-USE-009").Check(arch)).Message;
  Assert.Contains("Recording.SaveUseCase",message);Assert.Contains("Unresolved.SaveUseCase",message);Assert.DoesNotContain("Free.SaveUseCase",message);
 }
 [Fact] public void BoundaryCoverageHasDocumentedStaticLimit() {
  var layout=DcaLayout.ForRootNamespace(Root).WithFrameworkTypes(FrameworkTypes.AspNetCore() with { TransactionalAttribute=Root+".Module.Application.Declarative.WorkAttribute" });
  var arch=DcaArchitecture.Load(layout,typeof(EventPolicyTests).Assembly);var message=Assert.Throws<DcaRuleViolationException>(()=>Rule(arch,"DCA-USE-012").Check(arch)).Message;
  Assert.DoesNotContain("AttributedUseCase",message);
  Assert.Contains("UncoveredUseCase",message);Assert.DoesNotContain("AfterEmptyUseCase",message);Assert.DoesNotContain("CoveredUseCase",message);
 }
 [Fact] public void BusinessVersionIsAllowedButAdapterContractsAreNot() {
  var arch=Arch();Rule(arch,"DCA-ADV-006").Check(arch);Rule(arch,"DCA-ADV-007").Check(arch);
  Assert.Contains("Misplaced",Assert.ThrowsAny<System.Exception>(()=>Rule(arch,"DCA-STR-007").Check(arch)).Message);
 }
}
