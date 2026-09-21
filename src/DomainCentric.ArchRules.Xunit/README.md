# DomainCentric.ArchRules.Xunit

xUnit base class that runs the whole `DomainCentric.ArchRules` catalog as one theory case per rule.

```csharp
public sealed class ArchitectureTest : DcaArchitectureTest
{
    protected override DcaLayout Layout => DcaLayout.ForRootNamespace("Acme.Shop");

    protected override IEnumerable<Assembly> Assemblies => new[] { typeof(Program).Assembly };
}
```

## Requires xUnit v2

This package builds on `xunit.core` 2.x. On an xUnit **v3** project it restores and compiles without a
warning and then contributes no test at all: `dotnet test` exits 0, no architecture rule runs, and the
build is green while nothing was checked. The package therefore refuses that combination with build
error `DCA0001`.

On xUnit v3, reference `DomainCentric.ArchRules` alone and run the catalog from a test of your own:

```csharp
[Fact]
public void ArchitectureRulesHold()
{
    var layout = DcaLayout.ForRootNamespace("Acme.Shop");
    var architecture = DcaArchitecture.Load(layout, typeof(ArchitectureTests).Assembly);
    DcaRules.CheckAll(architecture, DcaRuleSelection.All());
}
```

Full documentation: https://github.com/domain-centric-development/dca-dotnet
