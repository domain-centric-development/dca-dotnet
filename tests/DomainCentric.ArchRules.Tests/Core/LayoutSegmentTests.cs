using System;
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
}
