using System;
using System.Linq;
using Xunit;
namespace DomainCentric.ArchRules.Tests.Rules;
public class DomainMetadataTests {
    private const string Root="DomainCentric.ArchRules.Tests.Fixtures.Metadata";
    private static DcaArchitecture Arch(string suffix) => DcaArchitecture.Load(
        DcaLayout.ForRootNamespace(Root+suffix).WithFrameworkTypes(FrameworkTypes.None() with {
            ContainerAttributeNamespaces=new[]{Root+".Roles.Container"},
            PersistenceAttributeNamespaces=new[]{Root+".Roles.Persistence"},
            TransactionAttributeNamespaces=new[]{Root+".Roles.Transaction"},
            InjectionAttributeNamespaces=new[]{Root+".Roles.Injection"},
            PersistenceAttributeTypes=FrameworkTypes.AspNetCore().PersistenceAttributeTypes
        }),typeof(DomainMetadataTests).Assembly);
    [Theory]
    [InlineData("DCA-ONI-003","Model")]
    [InlineData("DCA-ADV-004","Event")]
    [InlineData("DCA-ADV-011","Service")]
    [InlineData("DCA-ADV-015","Factory")]
    [InlineData("DCA-ADV-018","Spec")]
    public void EveryCellAndDerivedAttributeIsReportedByExactlyOneOwner(string id,string group) {
        var arch=Arch(".Bad");
        var rule=DcaRules.All(arch.Layout).Single(r=>r.Id==id);
        var message=Assert.Throws<DcaRuleViolationException>(()=>rule.Check(arch)).Message;
        foreach(var cell in new[]{"TypeContainer","TypePersistence","TypeTransaction","FieldInjection","FieldPersistence","PropertyInjection","PropertyPersistence","MethodTransaction","ConstructorInjection","TypeComposed"}) Assert.Contains(group+cell,message);
        if(group!="Event") Assert.Contains(group+"MethodInjection",message);
        if(group=="Model") Assert.Contains("ModelKeyProperty.Id carries prohibited metadata System.ComponentModel.DataAnnotations.KeyAttribute",message);
        Assert.DoesNotContain("ModelRequiredProperty",message);
        foreach(var other in new[]{"Model","Event","Service","Factory","Spec"}.Where(g=>g!=group)) Assert.DoesNotContain("Model."+other,message);
        var good=Arch(".Good");
        DcaRules.All(good.Layout).Single(r=>r.Id==id).Check(good);
    }
    [Fact] public void PresetsExposeMetadataRoles() {
        Assert.Contains("System.ComponentModel.DataAnnotations.Schema",FrameworkTypes.AspNetCore().PersistenceAttributeNamespaces);
        Assert.Contains("Microsoft.Extensions.DependencyInjection",FrameworkTypes.AspNetCore().InjectionAttributeNamespaces);
        Assert.Contains("System.ComponentModel.DataAnnotations.KeyAttribute",FrameworkTypes.AspNetCore().PersistenceAttributeTypes);
        Assert.Empty(FrameworkTypes.None().PersistenceAttributeTypes);
        Assert.Empty(FrameworkTypes.None().PersistenceAttributeNamespaces);
        Assert.Empty(FrameworkTypes.None().InjectionAttributeNamespaces);
    }
}
