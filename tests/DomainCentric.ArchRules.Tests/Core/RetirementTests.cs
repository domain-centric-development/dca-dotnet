using System;
using System.Linq;
using System.Collections.Generic;
using ArchUnitNET.Loader;
using DomainCentric.ArchRules.Rules;
using DomainCentric.ArchRules.ContextMap;
using Xunit;
namespace DomainCentric.ArchRules.Tests.Core;
public class RetirementTests {
 [Fact] public void PublishedExclusionsAndSeverityEntriesStillLoadAndAreReportedAsRetired() {
  foreach (var id in DcaRules.Retired().Keys) {
   Assert.DoesNotContain(id,DcaRules.AllIds());
   Assert.Contains(id,DcaRuleSelection.All().Excluding(id).Warning(id).RetiredReferences);
   Assert.Contains(id,DcaRuleSelection.FromProperties(new Dictionary<string,string> {{"dca.rules.off",id},{"dca.rules.warn",id}}).RetiredReferences);
   Assert.Contains(id,DcaRuleSelection.All().MergedWith(DcaRuleSelection.All().Excluding(id)).RetiredReferences);
  }
  Assert.Empty(DcaRuleSelection.All().Excluding("DCA-USE-016").RetiredReferences);
  Assert.Throws<ArgumentException>(()=>DcaRuleSelection.All().Excluding("DCA-ADV-999"));
 }
 [Fact] public void RetiredIdentitiesCannotBeSelected() {
  foreach (var id in DcaRules.Retired().Keys) {
   var message=Assert.Throws<ArgumentException>(()=>DcaRuleSelection.All().OnlyIds(id)).Message;
   Assert.Contains(id,message); Assert.Contains(DcaRules.Retired()[id].Replacement,message);
   Assert.Throws<ArgumentException>(()=>DcaRuleSelection.FromProperties(new Dictionary<string,string> {{"dca.rules.ids",id}}));
  }
 }
 [Fact] public void ConsumerImplementationInReservedOutputPackageFails() {
  var layout=DcaLayout.ForRootNamespace("Example");
  var model=new ArchLoader().LoadNamespacesWithinAssembly(typeof(DomainCentric.BuildingBlocks.Hexagonal.Ports.Out.ConsumerImplementation).Assembly,"DomainCentric.BuildingBlocks.Hexagonal.Ports.Out").Build();
  var arch=DcaArchitecture.Of(layout,model,typeof(DomainCentric.BuildingBlocks.Hexagonal.Ports.Out.ConsumerImplementation).Assembly);
  Assert.Contains("ConsumerImplementation",Assert.Throws<DcaRuleViolationException>(()=>new LayeredRules(layout).OutputPortMarkersMustBeInterfaces().Check(arch)).Message);
 }
 [Fact] public void ConsumerImplementationInReservedOutputPackageFailsThroughLoad() {
  var layout=DcaLayout.ForRootNamespace("Example");
  var arch=DcaArchitecture.Load(layout,typeof(DomainCentric.BuildingBlocks.Hexagonal.Ports.Out.ConsumerImplementation).Assembly);
  Assert.Contains("ConsumerImplementation",Assert.Throws<DcaRuleViolationException>(()=>new LayeredRules(layout).OutputPortMarkersMustBeInterfaces().Check(arch)).Message);
 }
 [Fact] public void InformationalEntriesHaveExplicitKind() {
  Assert.Equal(new[]{"DCA-LAY-001","DCA-MAP-013","DCA-STR-001","DCA-STR-010"},DcaRules.All(DcaLayout.ForRootNamespace("Example")).Where(r=>r.Kind==DcaRuleKind.Informational).Select(r=>r.Id).OrderBy(x=>x));
 }
 [Fact] public void RendererDisambiguatesExternalNames() {
  var arch=DcaArchitecture.Load(DcaLayout.ForRootNamespace("DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Bad"),typeof(RetirementTests).Assembly);
  var md=ContextMapRenderer.Of(arch).Render(); Assert.Contains("_2[[",md);
 }
}
