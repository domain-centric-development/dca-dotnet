using System;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
namespace DomainCentric.ArchRules.Tests.Rules;
public class OperationPolicyTests {
    private const string Root="DomainCentric.ArchRules.Tests.Fixtures.Operations.";
    private static DcaArchitecture Arch(string suffix)=>DcaArchitecture.Load(DcaLayout.ForRootNamespace(Root+suffix),typeof(OperationPolicyTests).Assembly);
    private static IDcaRule Rule(DcaArchitecture arch,string id)=>DcaRules.All(arch.Layout).Single(r=>r.Id==id);
    [Fact] public void InvocationFindsDirectPortAndTransitiveHelpers() {
        var arch=Arch("Bad");
        var message=Assert.Throws<DcaRuleViolationException>(()=>Rule(arch,"DCA-USE-016").Check(arch)).Message;
        foreach(var name in new[]{"DirectCallerUseCase","PortCallerUseCase","Flat.HelperCallerUseCase","Feature.Grouped.HelperCallerUseCase","[via "}) Assert.Contains(name,message);
        var good=Arch("Good"); Rule(good,"DCA-USE-016").Check(good);
    }
    [Theory][InlineData("Coord")][InlineData("Cycle")]
    public void CallerSideExclusionPermitsOnlyCoordinatorAndNeverRemovesCycles(string kind) {
        var arch=Arch(kind);var rule=Rule(arch,"DCA-USE-016");
        Assert.Throws<DcaRuleViolationException>(()=>rule.Check(arch));
        var caller=Root+kind+".Module.Application."+(kind=="Cycle"?"Feature.":"")+"Coordinator.CoordinatorUseCase";
        var selection=DcaRuleSelection.All().IgnoringViolationsMatching(rule.Id,"^"+Regex.Escape(caller)+" -> ");
        var outcome=DcaRuleExecution.Execute(rule,arch,selection);
        Assert.Equal(kind=="Coord"?DcaRuleStatus.Passed:DcaRuleStatus.Failed,outcome.Status);
        if(kind=="Cycle") Assert.Throws<DcaRuleViolationException>(()=>Rule(arch,"DCA-CYC-005").Check(arch));
    }
    [Fact] public void EffectivePublicSurfaceIncludesPropertiesAndInheritedAndUnrelatedMethods() {
        var arch=Arch("SurfaceBad");var message=Assert.Throws<DcaRuleViolationException>(()=>Rule(arch,"DCA-USE-017").Check(arch)).Message;
        foreach(var name in new[]{"ExtraUseCase","InheritedUseCase","UnrelatedUseCase","Value","Computed"}) Assert.Contains(name,message);
        var good=Arch("SurfaceGood");Rule(good,"DCA-USE-017").Check(good);
    }
    [Fact] public void GenericAsyncContractAcceptsExplicitAndInheritedImplementations() {
        var arch=Arch("SurfaceGood");
        // Property-only input ports are not generic use cases; isolate the generic implementations.
        var layout=DcaLayout.ForRootNamespace(Root+"SurfaceGood");
        foreach(var name in new[]{"ExplicitUseCase","InheritedUseCase","OrdinaryUseCase","GeneratedUseCase"}) {
            var type=typeof(OperationPolicyTests).Assembly.GetType(Root+"SurfaceGood.Module.Application.Operation."+name)!;
            Assert.Contains(type.GetInterfaces(),i=>i.IsGenericType&&i.GetGenericTypeDefinition()==typeof(DomainCentric.BuildingBlocks.Hexagonal.Ports.In.IUseCase<,>));
        }
        var result=DcaRuleExecution.Execute(Rule(arch,"DCA-NET-003"),arch,DcaRuleSelection.All().IgnoringViolationsMatching("DCA-NET-003","PropertyUseCase"));
        Assert.Equal(DcaRuleStatus.Passed,result.Status);
    }
    [Fact] public void AclEvidenceBelongsToEachUpstreamWithinOneAdapterPackage() {
        var root="DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.";
        var good=DcaArchitecture.Load(DcaLayout.ForRootNamespace(root+"Good"),typeof(OperationPolicyTests).Assembly);
        Rule(good,"DCA-MAP-008").Check(good);
        var bad=DcaArchitecture.Load(DcaLayout.ForRootNamespace(root+"Bad"),typeof(OperationPolicyTests).Assembly);
        var message=Assert.Throws<DcaRuleViolationException>(()=>Rule(bad,"DCA-MAP-008").Check(bad)).Message;
        Assert.Contains("towards 'V'",message);Assert.DoesNotContain("towards 'U'",message);
    }
}
