# dca-dotnet

.NET libraries for **Domain-Centric Architecture (DCA)** — a synthesis of Domain-Driven Design,
Hexagonal Architecture and Clean Architecture. The .NET twin of [dca-java](https://github.com/domain-centric-development/dca-java):
same building blocks, same rule ids.

*Written with AI assistance — drafted mainly by Claude, reviewed and directed by the author since
2025. The architecture rules in this repository's build are part of how that work is verified.*

| Package | What it is | Dependencies |
|---------|------------|--------------|
| `DomainCentric.BuildingBlocks` | The building blocks your code implements: DDD tactical markers (`IAggregateRoot`, `IEntity`, `IValue`, `IDomainEvent`, …), strategic attributes (`[BoundedContext]`, `[SharedKernel]`, `[Upstream]`, `[Partnership]`, …) and hexagonal port interfaces (`IUseCase`, `IRepository`, `IStore`, …) | none (`netstandard2.1`, `net8.0`, `net10.0`) |
| `DomainCentric.ArchRules` | The governance rules: ~100 ArchUnitNET rules pinned to those building blocks, plus an executable context map | `DomainCentric.BuildingBlocks`, ArchUnitNET (`net8.0`, `net10.0`) |
| `DomainCentric.ArchRules.Xunit` | xUnit base class — one theory case per rule | `DomainCentric.ArchRules`, xunit.core |

Versions are independent per package family; see [Versioning](#versioning).

## Quick start

### 1. Building blocks in production code

```
dotnet add package DomainCentric.BuildingBlocks
```

```csharp
namespace Acme.Shop.Cart.Domain.Model;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

public readonly record struct CartId(Guid Value) : IId;

public sealed class ShoppingCart : AggregateRootBase<ShoppingCart, CartId> { … }
```

C# has no `package-info`; a context is declared by one **marker class** in the context's root namespace:

```csharp
namespace Acme.Shop.Cart;

[BoundedContext("Shopping Cart", Description = "Carts and their items")]
[Upstream("Product", Translation.AntiCorruptionLayer, Consumes.Api,
          Rationale = "Product data is translated into cart's own types")]
public static class CartContext { }
```

Namespaces:

```
DomainCentric.BuildingBlocks
├── Ddd.Tactical                 IAggregateRoot, AggregateRootBase, IEntity, IValue, IId, IDomainEvent,
│                                IIntegrationEvent, [IntegrationEventType], IDomainService, IDomainGateway,
│                                IFactory, ISpecification<T>
├── Ddd.Strategic                [BoundedContext]
├── Ddd.Strategic.Relationships  [SharedKernel], [OpenHostService], [Upstream], [ExternalUpstream], [Partnership]
└── Hexagonal.Ports.In / .Out    IInputPort, IUseCase<TIn,TOut>  |  IOutputPort, IRepository, IStore,
                                 IDomainEventPublisher, IIntegrationEventPublisher
```

Ports are **async only** (`Task<TOut> ExecuteAsync(TIn, CancellationToken)`, `FindByIdAsync`, …); the
domain layer stays synchronous — a rule enforces it.

### 2. Rules in an architecture test

```
dotnet add package DomainCentric.ArchRules.Xunit
```

```csharp
using System.Reflection;
using DomainCentric.ArchRules;
using DomainCentric.ArchRules.Xunit;

public sealed class ArchitectureTest : DcaArchitectureTest
{
    protected override DcaLayout Layout => DcaLayout.ForRootNamespace("Acme.Shop");

    protected override IEnumerable<Assembly> Assemblies => new[] { typeof(Program).Assembly };
}
```

That is the whole test. Every rule runs as its own theory case, named by id (`DCA-TAC-001`, …). One
assembly per bounded context? Pass them all — the rules work on namespaces.

### 3. Adapt to your layout

```csharp
DcaLayout.ForRootNamespace("Acme.Shop")
    .WithIncomingSegment("In")                 // Adapter.In instead of Adapter.Incoming
    .WithOutgoingSegment("Out")
    .WithUseCaseSuffix("ApplicationService")
    .WithRestControllerSuffix("Endpoint")
    .AllowingInDomain("NodaTime")              // extra namespaces allowed in the domain
    .WithFrameworkTypes(FrameworkTypes.AspNetCore());   // or your own full names
```

### Choose which rules run, and how strictly

The catalog is opinionated, and no team adopts all of it on day one. A rule you disagree with, or
cannot satisfy yet, is a decision to record — not a reason to drop the library. `DcaRuleSelection`
expresses three things:

```csharp
protected override DcaRuleSelection AdditionalSelection => DcaRuleSelection.All()
    .OnlySets("cycles", "layered", "hexagonal")             // scope: adopt in stages
    .Excluding("DCA-NAM-005", "no MVC controllers here")    // off, with the reason
    .Warning("DCA-TAC-009", "being made sealed step by step")   // reported, does not fail the build
    .IgnoringViolationsMatching("DCA-STR-003", ".*Legacy.*");   // a documented exception
```

The same configuration can live in `dca-archunit.properties` next to the test assembly (copy it to the
output directory), which the base class reads and `AdditionalSelection` is then applied on top of —
the keys are identical to the Java library's:

```properties
dca.rules.sets              = cycles,layered,hexagonal
dca.rules.off               = DCA-NAM-005
dca.rule.DCA-NAM-005.reason = no MVC controllers here
dca.rules.warn              = DCA-TAC-009
dca.rules.warn.sets         = naming
dca.rule.DCA-STR-003.ignore = .*Legacy.*
```

An unknown rule id or set name fails the run immediately — a typo must never leave a rule silently
enforced.

Both sources combine: the file is the base, `AdditionalSelection` is merged on top, and the later entry
wins per rule id. Override `AdditionalSelection`, not `Selection` — the latter *replaces* the file, so
a `dca-archunit.properties` added later would be ignored without a word.

**Three differences from the Java library.** There is no baseline dial (`frozen` /
`dca.rules.freeze`): ArchUnitNET has no equivalent of ArchUnit's `FreezingArchRule`, and the properties
key is rejected with a message saying so — lower those rules to a warning instead. Because xUnit v2
cannot skip a test dynamically, a rule at `WARN` or `OFF` is reported green rather than skipped. And
only the **file** shapes the report: theory cases are built statically, before an instance exists, so a
rule the file scopes out produces no case at all and one it lowers is named
`naming / DCA-NAM-005 [OFF: no MVC controllers here]`. The same settings made in `AdditionalSelection`
take effect but appear only in the test output — put them in the file when the report should carry the
decision.

The older `ExcludedRuleIds` and `Rules` overrides keep working and feed the same selection.

Without xUnit (NUnit, MSTest, a console):

```csharp
var arch = DcaArchitecture.Load(DcaLayout.ForRootNamespace("Acme.Shop"), typeof(Program).Assembly);
DcaRules.CheckAll(arch);                       // or CheckAll(arch, selection) with a selection
```

Violations are thrown as `DcaRuleViolationException`.

### 4. Run against Debug builds

`DcaArchitecture.Load` refuses Release-built assemblies. Reason: in optimized builds the compiler emits
async state machines as structs, and ArchUnitNET drops those compiler-generated value types — every
dependency that occurs only inside an `async` method body would be invisible to the rules. `dotnet test`
builds Debug by default, so nothing changes for most projects; if you must analyse optimized assemblies,
opt in with `DcaArchitecture.Load(layout, allowOptimizedAssemblies: true, assemblies)` and accept the
blind spot.

## Rule catalog

Rule sets and identifier prefixes (identical to dca-java, so guide, catalog and tooling can speak of
one rule across both languages):

| Set | Prefix | Covers |
|-----|--------|--------|
| `layered` | `DCA-LAY` | layer dependency direction (domain ← application ← adapter ← infrastructure) |
| `onion` | `DCA-ONI` | onion view: nothing inside depends on anything outside |
| `hexagonal` | `DCA-HEX` | ports and adapters: input/output ports, adapter direction, no infrastructure leaks |
| `tactical` | `DCA-TAC` | aggregates, entities, value objects, ids, domain events, repositories |
| `strategic` | `DCA-STR` | bounded-context isolation, shared kernel, open host services, integration events |
| `contextmap` | `DCA-MAP` | `[Upstream]` / `[Partnership]` / `[ExternalUpstream]` declarations ⇔ real dependencies |
| `advanced` | `DCA-ADV` | domain services, factories, specifications, event hygiene, integration event types |
| `usecase` | `DCA-USE` | one use case per namespace, `I*InputPort` / `*UseCase` / `*Command` / `*Result` shape |
| `naming` | `DCA-NAM` | naming conventions, forbidden technical suffixes and bucket namespaces |
| `cycles` | `DCA-CYC` | no cycles between contexts, layers, use-case namespaces |
| `dotnet` | `DCA-NET` | .NET only: synchronous domain, `Async` suffix on port methods, one `ExecuteAsync`, records for values and ids |

The full list with rationale is in [RULES.md](RULES.md) (generated — `dotnet run --project tools/RulesCatalog -- .`).
Java rules that have no .NET counterpart (they only check a Spring annotation) are listed there as
*not applicable*; see [PORTING-LOG.md](PORTING-LOG.md) for the reasoning per rule.

## Beyond rules: executable context map

The same `[Upstream]` / `[ExternalUpstream]` / `[Partnership]` declarations that the `contextmap` rules
verify can be rendered into a markdown context map (tables + Mermaid diagram). Rendering is **opt-in**
— call it from a test of your own:

```csharp
[Fact]
public void RenderContextMap() =>
    ContextMapRenderer.Of(Architecture)
        .IncludeExternalSystems(true)
        .IncludePlanned(true)
        .WriteTo("docs/context-map.md");
```

Because the rules guarantee declarations match the code, the rendered map cannot drift.

## Versioning

Semantic versioning, independent per package family:

- `DomainCentric.BuildingBlocks` — rarely changes; a new marker is a minor bump, a removed or renamed
  one a major bump.
- `DomainCentric.ArchRules` (+ `.Xunit`, released together) — a new rule is a minor bump (it can fail
  your build — pin versions), a tightened rule is a major bump, a relaxed rule or fixed false positive
  a patch.

Tags: `building-blocks/vX.Y.Z`, `archrules/vX.Y.Z`. Changelogs: [src/DomainCentric.BuildingBlocks/CHANGELOG.md](src/DomainCentric.BuildingBlocks/CHANGELOG.md), [src/DomainCentric.ArchRules/CHANGELOG.md](src/DomainCentric.ArchRules/CHANGELOG.md).

## Build

```
dotnet build                                     # all packages + self-tests (Debug — see "Run against Debug builds")
dotnet test                                      # rule self-tests against Good/Bad fixtures
dotnet run --project tools/RulesCatalog -- .     # regenerate rules.json + RULES.md
dotnet pack -c Release -o artifacts              # try the packages in another project (local feed)
cd samples/MinimalConsumer && dotnet test        # the smallest consumer
```

## License

MIT — see [LICENSE](LICENSE).

Contributions are accepted under the MIT licence, and the copyright holder may additionally publish
them under other licences (for example a documentation licence for prose).
