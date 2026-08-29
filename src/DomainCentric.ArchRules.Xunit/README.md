# DomainCentric.ArchRules.Xunit

xUnit base class that runs the whole `DomainCentric.ArchRules` catalog as one theory case per rule.

```csharp
public sealed class ArchitectureTest : DcaArchitectureTest
{
    protected override DcaLayout Layout => DcaLayout.ForRootNamespace("Acme.Shop");

    protected override IEnumerable<Assembly> Assemblies => new[] { typeof(Program).Assembly };
}
```

Full documentation: https://github.com/domain-centric-development/dca-dotnet
