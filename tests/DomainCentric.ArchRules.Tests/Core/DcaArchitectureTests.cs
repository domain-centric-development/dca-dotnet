using System.Linq;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace DomainCentric.ArchRules.Tests.Core;

public sealed class DcaArchitectureTests
{
    private const string Root = "DomainCentric.ArchRules.Tests.Fixtures.Core";

    private static DcaArchitecture Arch() =>
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(Root), typeof(DcaArchitectureTests).Assembly);

    [Fact]
    public void DiscoversBoundedContextsInEncounterOrderAndSharedKernel()
    {
        var arch = Arch();
        Assert.Equal(new[] { Root + ".Cart", Root + ".Catalog" }, arch.BoundedContextNamespaces.OrderBy(x => x));
        Assert.Equal("carts", arch.BoundedContexts[Root + ".Cart"].Description);
        Assert.Equal(Root + ".SharedKernel", arch.SharedKernelNamespace);
        Assert.Equal(Root + ".Cart", arch.RootContextNamespace(Root + ".Cart.Domain.Model"));
        Assert.Null(arch.RootContextNamespace("Elsewhere.Cart"));
    }

    [Fact]
    public void ReadsRepeatableNamespaceAttributes()
    {
        var upstreams = Arch().NamespaceAttributes<UpstreamAttribute>(Root + ".Cart");
        Assert.Equal(2, upstreams.Count);
        Assert.Contains(upstreams, u => u.Context == "Pricing" && u.Status == UpstreamStatus.Planned);
    }

    [Fact]
    public void LoadsOnlyTypesBelowTheRootNamespace()
    {
        var arch = Arch();
        Assert.All(arch.Types, t => Assert.StartsWith(Root, t.FullName, System.StringComparison.Ordinal));
        Assert.Contains(arch.Classes, c => c.Name == "Cart");
    }

    [Fact]
    public void PatternsMatchArchUnitNetNamespaces()
    {
        var arch = Arch();
        var rule = DcaRule.Of("DCA-TEST-001", "domain classes are aggregate roots", "test",
            a => Classes().That().ResideInNamespaceMatching(a.Layout.DomainModelPattern).Should().BeAssignableTo(typeof(IAggregateRoot)))
            .Selecting("test").Checking("test");
        rule.Check(arch);

        var failing = DcaRule.Of("DCA-TEST-002", "domain classes are sealed records", "test",
            a => Classes().That().ResideInNamespaceMatching(a.Layout.DomainModelPattern).Should().BeRecord())
            .Selecting("test").Checking("test");
        var ex = Assert.Throws<DcaRuleViolationException>(() => failing.Check(arch));
        Assert.Contains("Cart", ex.Message, System.StringComparison.Ordinal);

        var empty = DcaRule.Of("DCA-TEST-003", "nothing selected passes", "test",
            a => Classes().That().HaveNameEndingWith("Nothing").Should().BeSealed())
            .Selecting("test").Checking("test");
        empty.Check(arch);
    }

    [Fact]
    public void CatalogIdsAreUniqueAndWellFormed()
    {
        var ids = DcaRules.AllIds();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.All(ids, id => Assert.Matches(@"^DCA-[A-Z]{3}-\d{3}$", id));
    }
}

public sealed class AsyncDependencyTests
{
    private const string Root = "DomainCentric.ArchRules.Tests.Fixtures.Core";

    [Fact]
    public void DependenciesInsideAsyncBodiesAreVisibleInDebugBuilds()
    {
        var assembly = typeof(AsyncDependencyTests).Assembly;
        Assert.False(DcaArchitecture.IsJitOptimized(assembly), "run the self-tests in Debug configuration");
        var arch = DcaArchitecture.Load(DcaLayout.ForRootNamespace(Root), assembly);
        var caller = arch.Classes.Single(c => c.Name == "AsyncCaller");
        Assert.Contains(caller.Dependencies, d => d.Target.Name == "NoContextHere");
    }
}
