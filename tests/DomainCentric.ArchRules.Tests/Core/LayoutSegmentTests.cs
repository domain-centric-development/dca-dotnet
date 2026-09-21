using System;
using System.Linq;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Core;

/// <summary>
/// The web adapter and shared segments are configurable like every other one: <c>DCA-NAM-011</c>
/// reads the first, the shared-output-port patterns read the second, and the shared segment stays
/// reserved against the operation containers whatever it is called.
/// </summary>
public class LayoutSegmentTests
{
    private static readonly DcaLayout Defaults = DcaLayout.ForRootNamespace("Acme.Shop");

    [Fact]
    public void TheWebAdapterAndSharedSegmentsAreConfigurable()
    {
        Assert.Equal("Web", Defaults.WebSegment);
        Assert.Equal("Shared", Defaults.SharedSegment);
        Assert.Equal("Ui", Defaults.WithWebSegment("Ui").WebSegment);

        var ports = Defaults.WithSharedSegment("Ports");
        Assert.Contains("Ports", ports.SharedOutputPortPattern, StringComparison.Ordinal);
        Assert.DoesNotContain("Shared", ports.SharedOutputPortPattern, StringComparison.Ordinal);
        Assert.Contains("Ports", ports.SharedOutputPortPatternOf("Acme.Shop.Cart"), StringComparison.Ordinal);
    }

    [Fact]
    public void TheSharedSegmentStaysReservedWhateverItIsCalled()
    {
        Assert.Throws<ArgumentException>(() => Defaults.WithOperationContainers("Shared"));
        Assert.Throws<ArgumentException>(() => Defaults.WithSharedSegment("Ports").WithOperationContainers("Ports"));
        Assert.Throws<ArgumentException>(() => Defaults.WithWebSegment(string.Empty));
        Assert.Throws<ArgumentException>(() => Defaults.WithSharedSegment("  "));
    }

    /// <summary>
    /// The tactical suffixes and the domain-service segment are configurable like the use-case and
    /// controller suffixes; a project whose ports are called something else configures them rather
    /// than excluding the rule ids.
    /// </summary>
    [Fact]
    public void TheTacticalNamesAreConfigurable()
    {
        Assert.Equal("AggregateRoot", Defaults.AggregateRootSuffix);
        Assert.Equal("Repository", Defaults.RepositorySuffix);
        Assert.Equal("Store", Defaults.StoreSuffix);
        Assert.Equal("Factory", Defaults.FactorySuffix);
        Assert.Equal("Specification", Defaults.SpecificationSuffix);

        var own = Defaults
            .WithAggregateRootSuffix("Root")
            .WithRepositorySuffix("Gateway")
            .WithStoreSuffix("Table")
            .WithFactorySuffix("Builder")
            .WithSpecificationSuffix("Rule");

        Assert.Equal("Root", own.AggregateRootSuffix);
        Assert.Equal("Gateway", own.RepositorySuffix);
        Assert.Equal("Table", own.StoreSuffix);
        Assert.Equal("Builder", own.FactorySuffix);
        Assert.Equal("Rule", own.SpecificationSuffix);
        Assert.Equal("Acme.Shop", own.RootNamespace);

        Assert.Throws<ArgumentException>(() => Defaults.WithRepositorySuffix(string.Empty));
        Assert.Throws<ArgumentException>(() => Defaults.WithStoreSuffix("  "));
    }

    /// <summary>
    /// The suffix drives the selection: with the repository suffix changed, the bad tactical
    /// fixture's <c>*Repository</c> interfaces are no longer selected and the name-anchored rules
    /// pass — while the default suffix still reports them, so the assertion is not vacuous.
    /// </summary>
    [Fact]
    public void TheRepositorySuffixDrivesTheSelection()
    {
        const string bad = "DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad";
        var own = DcaLayout.ForRootNamespace(bad).WithRepositorySuffix("Gateway");
        var arch = DcaArchitecture.Load(own, typeof(LayoutSegmentTests).Assembly);

        foreach (var id in new[] { "DCA-TAC-013", "DCA-TAC-016" })
        {
            DcaRules.All(own).Single(r => r.Id == id).Check(arch);
        }

        var plain = DcaLayout.ForRootNamespace(bad);
        var message = Assert.Throws<DcaRuleViolationException>(
            () => DcaRules.All(plain).Single(r => r.Id == "DCA-TAC-016")
                .Check(DcaArchitecture.Load(plain, typeof(LayoutSegmentTests).Assembly))).Message;
        Assert.Contains("Repository", message, StringComparison.Ordinal);
    }
    [Fact]
    public void TheTimestampTypesAreConfigurable()
    {
        Assert.Equal(new[] { "System.DateTimeOffset", "System.DateTime" }, Defaults.TimestampTypes);

        var own = Defaults.WithTimestampTypes("Acme.Shop.Platform.Timestamp");
        Assert.Equal(new[] { "Acme.Shop.Platform.Timestamp" }, own.TimestampTypes);
        Assert.Equal("Acme.Shop", own.RootNamespace);

        // An empty list would report every event, which is never what a caller means.
        Assert.Throws<ArgumentException>(() => Defaults.WithTimestampTypes());
        Assert.Throws<ArgumentException>(() => Defaults.WithTimestampTypes("  "));
    }
}
