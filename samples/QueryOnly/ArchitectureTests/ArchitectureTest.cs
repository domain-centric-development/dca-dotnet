using DomainCentric.ArchRules;using Xunit;using Xunit.Abstractions;
public class ArchitectureTest(ITestOutputHelper output) {
 [Fact] public void SelectedCatalogPasses(){
  var layout=DcaLayout.ForRootNamespace("Acme").WithFrameworkTypes(FrameworkTypes.None());
  var arch=DcaArchitecture.Load(layout,typeof(Acme.Ledger.Application.Read.ReadUseCase).Assembly);
  var selected=DcaRules.SelectFlat(layout,DcaRuleSelection.All().OnlySets("usecase","onion","hexagonal","naming","advanced","cycles","tactical","dotnet")).ToArray();
  output.WriteLine("Selected rules: "+selected.Length);
  foreach(var rule in selected)rule.Check(arch);
 }
}
