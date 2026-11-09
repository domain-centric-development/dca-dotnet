# DomainCentric.ArchRules

Executable architecture rules of Domain-Centric Architecture on ArchUnitNET, in the sets `layered`,
`onion`, `hexagonal`, `tactical`, `strategic`, `contextmap`, `advanced`, `usecase`, `naming`, `cycles`,
`errors` and the .NET-only `dotnet`. Rule ids `DCA-<SET>-<NNN>` are shared with the Java library `dca-archunit`.

```csharp
var arch = DcaArchitecture.Load(DcaLayout.ForRootNamespace("Acme.Shop"), typeof(Program).Assembly);
DcaRules.CheckAll(arch);
```

For xUnit use the companion package `DomainCentric.ArchRules.Xunit`. Full documentation:
https://github.com/domain-centric-development/dca-dotnet
