using System.Collections.Generic;
using System.Reflection;
using DomainCentric.ArchRules.Xunit;

namespace DomainCentric.ArchRules.Tests.Core;

/// <summary>
/// The real entry point on the application <c>dotnet new web</c> writes: the xUnit base class over an assembly whose
/// only type is the generated <c>Program</c>. The runner executes the whole catalog; every case passes.
/// </summary>
public sealed class GeneratedWebApplicationArchitectureTest : DcaArchitectureTest
{
    protected override DcaLayout Layout => DcaLayout.ForRootNamespace("Acme.Web");

    protected override IEnumerable<Assembly> Assemblies => new[] { EntryPointTests.Web };
}
