# Minimal consumer

The smallest .NET project that uses both packages: one bounded context (`Acme.Shop.Cart`) with an
aggregate, a use case, a repository port and its adapter, plus one architecture test class that runs
the whole DCA rule catalog:

```csharp
public sealed class ArchitectureTest : DcaArchitectureTest
{
    protected override DcaLayout Layout => DcaLayout.ForRootNamespace("Acme.Shop");

    protected override IEnumerable<Assembly> Assemblies => new[] { typeof(Cart.CartContext).Assembly };
}
```

```
dotnet test
```

Inside this repository the sample references the library projects; copied elsewhere it resolves the
NuGet packages `DomainCentric.BuildingBlocks` and `DomainCentric.ArchRules.Xunit`.
