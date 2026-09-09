using DomainCentric.ArchRules.Rules;
using Xunit;
namespace DomainCentric.ArchRules.Tests.Rules;
public class OperationContainerTests {
 [Theory]
 [InlineData("DCA-NAM-010")]
 [InlineData("DCA-USE-008")]
 [InlineData("DCA-TAC-014")]
 [InlineData("DCA-TAC-019")]
 [InlineData("DCA-TAC-021")]
 public void LocalPortsOutgoingResponsesAndDomainManagersAreAllowed(string id) {
  var layout = DcaLayout.ForRootNamespace("DomainCentric.ArchRules.Tests.Fixtures.Conventions.Containers");
  System.Linq.Enumerable.Single(DcaRules.All(layout), r => r.Id == id).Check(DcaArchitecture.Load(layout, typeof(OperationContainerTests).Assembly));
 }
 [Fact] public void ContainerCycleUsesMarkerDiscovery() {
  var layout = DcaLayout.ForRootNamespace("DomainCentric.ArchRules.Tests.Fixtures.Conventions.Cycles").WithOperationContainers("UseCases");
  Assert.Throws<DcaRuleViolationException>(() => CycleRules.ApplicationSlicesFreeOfCycles(layout).Check(DcaArchitecture.Load(layout, typeof(OperationContainerTests).Assembly)));
 }
 [Fact] public void ConstructionChecksCallerRoleAndContextNotVisibility() {
  var layout = DcaLayout.ForRootNamespace("DomainCentric.ArchRules.Tests.Fixtures.Construction");
  var message = Assert.Throws<DcaRuleViolationException>(() => TacticalPatternRules.EntitiesHaveNoPublicConstructors()
    .Check(DcaArchitecture.Load(layout, typeof(OperationContainerTests).Assembly))).Message;
  foreach (var caller in new[] { "CreationUseCase", "Persistence", "ForeignFactory" }) Assert.Contains(caller, message);
  Assert.DoesNotContain("LineFactory constructs", message);
  Assert.DoesNotContain("OtherAggregate constructs", message);
 }
 [Fact] public void ContainersNormalizeRootsAndKeepMixedLayoutsInvalid() {
  foreach (var kind in new[] { "Containers", "Mixed" }) {
   var plain = DcaLayout.ForRootNamespace("DomainCentric.ArchRules.Tests.Fixtures.Conventions." + kind);
   var configured = plain.WithOperationContainers("UseCases");
   DcaArchitecture Arch(DcaLayout layout) => DcaArchitecture.Load(layout, typeof(OperationContainerTests).Assembly);
   Assert.Throws<DcaRuleViolationException>(() => UseCaseRules.UseCasePackagesUseOneDepth(plain).Check(Arch(plain)));
   if (kind == "Containers") UseCaseRules.UseCasePackagesUseOneDepth(configured).Check(Arch(configured));
   else Assert.Throws<DcaRuleViolationException>(() => UseCaseRules.UseCasePackagesUseOneDepth(configured).Check(Arch(configured)));
  }
 }
}
