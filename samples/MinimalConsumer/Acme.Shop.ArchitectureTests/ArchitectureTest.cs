using System.Reflection;
using DomainCentric.ArchRules;
using DomainCentric.ArchRules.Xunit;

namespace Acme.Shop.ArchitectureTests;

/// <summary>The whole DCA rule catalog, one theory case per rule.</summary>
public sealed class ArchitectureTest : DcaArchitectureTest
{
    protected override DcaLayout Layout => DcaLayout.ForRootNamespace("Acme.Shop");

    protected override IEnumerable<Assembly> Assemblies => new[] { typeof(Cart.CartContext).Assembly };
}
