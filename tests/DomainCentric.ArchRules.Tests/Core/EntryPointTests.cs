using System;
using System.Linq;
using System.Reflection;
using DomainCentric.ArchRules.Rules;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Core;

/// <summary>
/// A type that only starts the program is no production code: the shapes, the generated web application, and one
/// real type beside it.
/// </summary>
public sealed class EntryPointTests
{
    private static readonly Assembly Shapes = Assembly.Load("DomainCentric.ArchRules.EntryPointShapes");

    internal static Assembly Web { get; } = Copied("DomainCentric.ArchRules.WebEntryPointFixture");

    internal static Assembly WebPlus { get; } = Copied("DomainCentric.ArchRules.WebEntryPointPlusFixture");

    /// <summary>A generated web application, copied beside the tests as a file rather than referenced.</summary>
    private static Assembly Copied(string name) =>
        Assembly.LoadFrom(System.IO.Path.Combine(AppContext.BaseDirectory, name + ".dll"));

    private static Type Shape(string name) => Shapes.GetType("Acme.Shapes." + name, throwOnError: true)!;

    [Theory]
    [InlineData("Program")]
    [InlineData("AsyncWithLambda")]
    public void AClassWhoseOnlyMethodIsMainIsAnEntryPoint(string name) =>
        Assert.True(DcaArchitecture.IsEntryPoint(Shape(name)));

    [Theory]
    [InlineData("WithField")]
    [InlineData("WithSecondMethod")]
    [InlineData("WithNestedType")]
    [InlineData("WithProperty")]
    [InlineData("WithoutMain")]
    public void AnyOtherMemberMakesItProductionCode(string name) =>
        Assert.False(DcaArchitecture.IsEntryPoint(Shape(name)));

    [Fact]
    public void TheProgramOfTopLevelStatementsIsAnEntryPoint()
    {
        var program = Web.GetType("Program", throwOnError: true)!;
        Assert.True(DcaArchitecture.IsEntryPoint(program));
    }

    [Fact]
    public void AGeneratedWebApplicationLoadsWithTheNotice()
    {
        var arch = DcaArchitecture.Load(DcaLayout.ForRootNamespace("Acme.Web"), Web);

        Assert.False(arch.HoldsProductionCode);
        Assert.Contains("without production code", arch.NoProductionCodeNotice, StringComparison.Ordinal);
    }

    [Fact]
    public void OneRealTypeBesideItTurnsTheGuardsRed()
    {
        var layout = DcaLayout.ForRootNamespace("Acme.Web");
        var arch = DcaArchitecture.Load(layout, WebPlus);

        Assert.True(arch.HoldsProductionCode);
        Assert.Null(arch.NoProductionCodeNotice);
        var rules = new StrategicPatternRules(layout).Rules;
        Assert.Throws<DcaRuleViolationException>(() => rules.Single(r => r.Id == "DCA-STR-011").Check(arch));
        Assert.Throws<DcaRuleViolationException>(() => rules.Single(r => r.Id == "DCA-STR-012").Check(arch));
    }

    [Fact]
    public void AMisspeltRootNamespaceOverRealCodeStillFails()
    {
        var refused = Assert.Throws<InvalidOperationException>(
            () => DcaArchitecture.Load(DcaLayout.ForRootNamespace("Acme.Typo"), WebPlus));

        Assert.Contains("Acme.Web.Greeting", refused.Message, StringComparison.Ordinal);
    }
}
