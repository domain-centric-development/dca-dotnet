using System.Collections.Generic;
using System.Reflection;
using DomainCentric.ArchRules.Xunit;

namespace DomainCentric.ArchRules.Tests.Core;

/// <summary>
/// The real entry point on a project without production code: the xUnit base class over an assembly that holds no
/// type at all. The runner executes the whole catalog; every case passes, and the layout diagnostics say why.
/// </summary>
public sealed class EmptyProjectArchitectureTest : DcaArchitectureTest
{
    /// <summary>The production assembly of a project before its first line.</summary>
    public static Assembly EmptyAssembly { get; } = Assembly.Load("DomainCentric.ArchRules.EmptyFixture");

    protected override DcaLayout Layout => DcaLayout.ForRootNamespace("Acme.Empty");

    protected override IEnumerable<Assembly> Assemblies => new[] { EmptyAssembly };
}
