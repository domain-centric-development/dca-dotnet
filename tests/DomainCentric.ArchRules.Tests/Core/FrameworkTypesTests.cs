using System.Linq;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Core;

/// <summary>
/// The rules resolve their framework vocabulary through <see cref="FrameworkTypes"/>: the ASP.NET Core
/// preset is the default, <see cref="FrameworkTypes.None"/> runs the whole catalog without any framework
/// type to look for, and the preset in use is named by the layout.
/// </summary>
public sealed class FrameworkTypesTests
{
    private const string Good = "DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good";

    [Fact]
    public void AspNetCoreIsTheDefaultAndNamed()
    {
        var layout = DcaLayout.ForRootNamespace(Good);
        Assert.Equal("aspnetcore", layout.FrameworkTypes.Name);
        Assert.Equal("aspnetcore", layout.FrameworkTypes.ToString());
        Assert.Contains("frameworkTypes=aspnetcore", layout.ToString());
    }

    [Fact]
    public void NoneHasEveryRoleEmpty()
    {
        var none = FrameworkTypes.None();
        Assert.False(FrameworkTypes.IsSet(none.ControllerBase));
        Assert.False(FrameworkTypes.IsSet(none.ApiControllerAttribute));
        Assert.False(FrameworkTypes.IsSet(none.PageModelBase));
        Assert.False(FrameworkTypes.IsSet(none.TransactionScope));
        Assert.True(FrameworkTypes.IsSet(FrameworkTypes.AspNetCore().TransactionScope));
    }

    [Fact]
    public void AWithExpressionAdjustsOneRoleAndKeepsTheName()
    {
        var adjusted = FrameworkTypes.AspNetCore() with { TransactionScope = "Acme.Platform.UnitOfWork" };
        Assert.Equal("Acme.Platform.UnitOfWork", adjusted.TransactionScope);
        Assert.Equal(FrameworkTypes.AspNetCore().ControllerBase, adjusted.ControllerBase);
        Assert.Equal("aspnetcore", adjusted.Name);
    }

    /// <summary>
    /// Without any framework type the catalog still runs to the end, and a fixture that does not lean on
    /// framework types yields the same outcomes as under the ASP.NET Core preset - the roles select
    /// less, never more, and never crash on an empty name.
    /// </summary>
    [Fact]
    public void TheCatalogRunsWithoutAnyFrameworkType()
    {
        var failingUnderNone = FailingRuleIds(FrameworkTypes.None());
        var failingUnderAspNetCore = FailingRuleIds(FrameworkTypes.AspNetCore());
        Assert.Equal(failingUnderAspNetCore, failingUnderNone);
    }

    private static string[] FailingRuleIds(FrameworkTypes types)
    {
        var layout = DcaLayout.ForRootNamespace(Good).WithFrameworkTypes(types);
        var arch = DcaArchitecture.Load(layout, typeof(FrameworkTypesTests).Assembly);
        return DcaRules.All(layout)
            .Select(rule => DcaRuleExecution.Execute(rule, arch, DcaRuleSelection.All()))
            .Where(outcome => outcome.Status == DcaRuleStatus.Failed)
            .Select(outcome => outcome.RuleId)
            .OrderBy(id => id, System.StringComparer.Ordinal)
            .ToArray();
    }
}
