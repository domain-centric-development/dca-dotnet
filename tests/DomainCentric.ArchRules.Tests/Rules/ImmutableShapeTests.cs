using System.Linq;
using Xunit;
namespace DomainCentric.ArchRules.Tests.Rules;
public sealed class ImmutableShapeTests {
 [Theory]
 [InlineData("DCA-TAC-009")]
 [InlineData("DCA-TAC-010")]
 [InlineData("DCA-TAC-012")]
 [InlineData("DCA-NET-004")]
 public void ImmutableClassesAndStructsWithEqualityPass(string id) {
   var layout = DcaLayout.ForRootNamespace("DomainCentric.ArchRules.Tests.Fixtures.Shape.Good");
   DcaRules.All(layout).Single(r => r.Id == id).Check(DcaArchitecture.Load(layout, typeof(ImmutableShapeTests).Assembly));
 }
 [Theory]
 [InlineData("DCA-USE-015", "StructResult")]
 [InlineData("DCA-TAC-010", "MutableValue")]
 [InlineData("DCA-TAC-010", "MutableRecordValue")]
 [InlineData("DCA-TAC-012", "MutableValue")]
 [InlineData("DCA-NET-004", "MutableValue")]
 [InlineData("DCA-NET-005", "MutableId")]
 [InlineData("DCA-USE-004", "StateCommand")]
 [InlineData("DCA-USE-004", "RecordCommand")]
 [InlineData("DCA-USE-005", "StateQuery")]
 [InlineData("DCA-USE-007", "StateResult")]
 [InlineData("DCA-ADV-001", "RecordChanged")]
 [InlineData("DCA-ADV-001", "StructChanged")]
 [InlineData("DCA-STR-008", "RecordChanged")]
 [InlineData("DCA-STR-008", "StructChanged")]
 public void ImmutableStateIsChecked(string id, string name) {
   foreach (var kind in new[] { "Good", "Bad" }) {
     var layout = DcaLayout.ForRootNamespace("DomainCentric.ArchRules.Tests.Fixtures.Shape." + kind);
     var arch = DcaArchitecture.Load(layout, typeof(ImmutableShapeTests).Assembly);
     var rule = DcaRules.All(layout).Single(r => r.Id == id);
     if (kind == "Good") rule.Check(arch);
     else Assert.Contains(name, Assert.ThrowsAny<System.Exception>(() => rule.Check(arch)).Message);
   }
 }
}
