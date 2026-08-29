# DomainCentric.ArchRules

Executable architecture rules of Domain-Centric Architecture on ArchUnitNET — layered, onion,
hexagonal, DDD tactical and strategic patterns, context map, use cases, naming, cycles. Rule ids
`DCA-<SET>-<NNN>` are shared with the Java library `dca-archunit`.

```csharp
var arch = DcaArchitecture.Load(DcaLayout.ForRootNamespace("Acme.Shop"), typeof(Program).Assembly);
DcaRules.CheckAll(arch);
```

For xUnit use the companion package `DomainCentric.ArchRules.Xunit`. Full documentation:
https://github.com/domain-centric-development/dca-dotnet
