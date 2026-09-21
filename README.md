# dca-dotnet

.NET libraries for **Domain-Centric Architecture (DCA)** — a synthesis of Domain-Driven Design,
Hexagonal Architecture and Clean Architecture. The .NET twin of [dca-java](https://github.com/domain-centric-development/dca-java):
same building blocks, same rule ids.

*Written with AI assistance — drafted mainly by Claude, reviewed and directed by Christoph Bloemer since
2025. The architecture rules in this repository's build are part of how that work is verified.*

| Package | What it is | Dependencies |
|---------|------------|--------------|
| `DomainCentric.BuildingBlocks` | The building blocks your code implements: DDD tactical markers (`IAggregateRoot`, `IEntity`, `IValue`, `IDomainEvent`, …), strategic attributes (`[BoundedContext]`, `[SharedKernel]`, `[Upstream]`, `[Partnership]`, …) and hexagonal port interfaces (`IUseCase`, `IRepository`, `IStore`, …) | none (`netstandard2.1`, `net8.0`, `net10.0`) |
| `DomainCentric.ArchRules` | The governance rules: 127 ArchUnitNET rules pinned to those building blocks, plus an executable context map | `DomainCentric.BuildingBlocks`, ArchUnitNET (`net8.0`, `net10.0`) |
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
│                                IFactory, ISpecification<T>, DomainException
├── Ddd.Strategic                [BoundedContext]
├── Ddd.Strategic.Relationships  [SharedKernel], [OpenHostService], [Upstream], [ExternalUpstream], [Partnership]
├── Hexagonal.Ports.In / .Out    IInputPort, IUseCase<TIn,TOut>  |  IOutputPort, IRepository, IStore,
│                                IDomainEventPublisher, IIntegrationEventPublisher
└── Application                  ITransactionBoundary (execution abstraction, not a port),
                                 UseCaseException
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
    .WithApiSegment("Contract")                // published sync contract (default: Api); events: WithEventsSegment
    .WithModelSegment("Entities")              // Domain.Entities instead of Domain.Model
    .WithIncomingEventSegment("Listener")      // event consumers under Adapter.Incoming.Listener (default: Event)
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
dca.rule.DCA-STR-003.ignore.1 = Generated.{1,3}Client
dca.framework               = none
```

`dca.framework` selects the framework preset by name — `aspnetcore` (the default) or `none`. An
unknown name fails and lists the known ones. A layout that names its framework types in code with
`WithFrameworkTypes(...)` always wins over the file.

An unknown rule id or set name fails the run immediately — a typo must never leave a rule silently
enforced. The value of an `.ignore` key is **one** regular expression, commas included; a second
expression for the same rule goes into an indexed key (`.ignore.1`, `.ignore.2`, …).

Both sources combine: the file is the base, `AdditionalSelection` is merged on top, and the later entry
wins per rule id. Override `AdditionalSelection`, not `Selection` — the latter *replaces* the file, so
a `dca-archunit.properties` added later would be ignored without a word.

**Four differences from the Java library.** There is no baseline dial yet (`frozen` /
`dca.rules.freeze`). ArchUnitNET does ship a `FreezingArchRule`, but the rules of this library do not
expose the ArchUnitNET rule behind them, so there is nothing to build a baseline from; the properties
key is rejected with a message saying so. **On an existing code base, use `dca.rule.<id>.ignore`
instead**: one regular expression over what is already broken, so the rule reports only what you break
from now on. It works for every rule, not only the ones a baseline could bind to, and it means the
same thing in both libraries. Because xUnit v2
cannot skip a test dynamically, a rule at `WARN` or `OFF` is reported green rather than skipped. And
only the **file** shapes the report: theory cases are built statically, before an instance exists, so a
rule the file scopes out produces no case at all and one it lowers is named
`naming / DCA-NAM-005 [OFF: no MVC controllers here]`. The same settings made in `AdditionalSelection`
take effect but appear only in the test output — put them in the file when the report should carry the
decision. Finally, there is **no framework detection and no preset SPI**: `FrameworkTypes` ships two
presets, `AspNetCore()` (the default) and `None()`, both selectable in code or with
`dca.framework=aspnetcore|none`, while the Java library ships five, recognises the one on the test
class path and lets a project register its own through `META-INF/services`. A project on a platform
neither preset covers builds its own `FrameworkTypes` in code. Which preset and which marker
vocabulary a run resolved is printed by the `LayoutDiagnostics` fact of `DcaArchitectureTest`, in the
same wording the Java report uses:

```
framework annotations: aspnetcore (default)
building block markers: dca (library default)
```

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

**127 rule ids in 12 sets: 121 enforced, 5 informational, 1 not applicable in .NET.** Three further ids are retired and keep their meaning in the retirement list. The
exact, current list is [RULES.md](RULES.md) and [rules.json](rules.json), both generated from the code
by `dotnet run --project tools/RulesCatalog -- .` and verified in CI; every number in this README is
taken from there. `rules.json` names the package version its texts belong to.

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
| `errors` | `DCA-ERR` | domain and use-case exceptions: base types, their layer, no framework or transport vocabulary |
| `dotnet` | `DCA-NET` | .NET only: synchronous domain, `Async` suffix on port methods, an awaitable and cancellable use-case contract, readonly structs for struct values and ids |

The full list with rationale is in [RULES.md](RULES.md) (generated — `dotnet run --project tools/RulesCatalog -- .`).
One Java rule has no .NET counterpart and is listed there as *not applicable*: `DCA-NAM-002`, a
diagnostic over the container stereotype, which .NET has no attribute for. Two ids that used to be
listed that way — `DCA-USE-013` and `DCA-MAP-006` — are carried here under the same id and select
nothing until the project configures the role they read, exactly as their Java twins select nothing
where the framework annotation is absent. See [PORTING-LOG.md](PORTING-LOG.md) for the reasoning per
rule.

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

## Compatibility

| Runs on | Version | Tested against |
|---|---|---|
| .NET | 8.0 and 10.0 | both |
| `DomainCentric.BuildingBlocks` | also `netstandard2.1` | — |
| ArchUnitNET | 0.13.4 | 0.13.4 |
| `DomainCentric.ArchRules.Xunit` | **xUnit v2 only** (`xunit.core` 2.x) | 2.9.3 |
| Build configuration | **Debug** | see *Run against Debug builds* |

**xUnit v3 is not supported by `DomainCentric.ArchRules.Xunit`.** The package compiles and restores
against a v3 project without a warning, and then the runner discovers nothing from it: `dotnet test`
exits 0, no architecture rule runs and the build stays green while nothing is checked. Since 0.5.0 the
package refuses that combination with build error `DCA0001` rather than letting it pass silently.

A v3 project runs the whole catalog without this package — reference `DomainCentric.ArchRules` alone
and call the catalog from a test of your own:

```csharp
[Fact]
public void ArchitectureRulesHold()
{
    var layout = DcaLayout.ForRootNamespace("Acme.Shop");
    var architecture = DcaArchitecture.Load(layout, typeof(ArchitectureTests).Assembly);
    DcaRules.CheckAll(architecture, DcaRuleSelection.All());
}
```

That works on any test framework. What it does not give you is one test per rule in the report, which
is the only thing the `.Xunit` package adds.

## Public API

What a consumer may rely on, and what may change in a patch:

| Public — covered by the versioning promise | Internal — may change without notice |
|---|---|
| The **rule ids** and their meaning, `rules.json` and `RULES.md` | The rule-set classes (`TacticalPatternRules`, `UseCaseRules`, …) and their factory methods |
| `DcaLayout`, `DcaMarkers`, `FrameworkTypes` and the preset SPI | The `Rules` namespace as a whole, and every helper in it |
| `DcaArchitecture`, `DcaRules`, `DcaRuleSelection`, `DcaRuleExecution`, `DcaRuleOutcome`, `DcaSeverity` | The wording of a violation message, beyond the `[DCA-XXX-nnn]` prefix every line carries |
| `IDcaRule` as a type to read — `Id`, `Title`, `Rationale`, `Selects`, `Checks` | The retired rules' implementations, which exist only so an old reference still compiles |
| `DomainCentric.BuildingBlocks` — every marker and port type | Test fixtures, and anything under an `Internal` namespace |
| The xUnit base class `DcaArchitectureTest` (xUnit v2 — see *Compatibility*) | |

The rule-set classes are `public` because the catalog tool and the xUnit integration live outside
their namespace, not because they are meant to be called directly. Build a run through `DcaRules` and
a `DcaRuleSelection`; that is the supported entry point, and the one that applies severities and
tolerated violations.

## Versioning

Semantic versioning, independent per package family:

- `DomainCentric.BuildingBlocks` — rarely changes; a new marker is a minor bump, a removed or renamed
  one a major bump.
- `DomainCentric.ArchRules` (+ `.Xunit`, released together) — **from 1.0 on, a new or tightened rule is
  a major bump.** Adding a rule can turn a green build red, and SemVer calls that breaking however small
  the change is. A relaxed rule, a fixed false positive and a clearer message are patches. **Before 1.0**
  a minor version may add and tighten rules, which is what 0.x is for: every such change is listed under
  *What can turn a green build red* in the changelog, with a migration note at the top of the release.
  Either way, pin the version.
- **Rule ids are the stable contract.** An id is never reused for a different check and never
  renumbered. A withdrawn rule keeps its id in the retirement list of `rules.json` with the reason and
  its replacement, so a `dca.rules.off` entry or a catalog reference never silently means something else.

Tags: `building-blocks/vX.Y.Z`, `archrules/vX.Y.Z`; the release procedure is in [RELEASING.md](RELEASING.md). Changelogs: [src/DomainCentric.BuildingBlocks/CHANGELOG.md](src/DomainCentric.BuildingBlocks/CHANGELOG.md), [src/DomainCentric.ArchRules/CHANGELOG.md](src/DomainCentric.ArchRules/CHANGELOG.md).

## Build

```
dotnet build                                     # all packages + self-tests (Debug — see "Run against Debug builds")
dotnet test                                      # rule self-tests against Good/Bad fixtures
dotnet run --project tools/RulesCatalog -- .     # regenerate rules.json + RULES.md
dotnet pack -c Release -o artifacts              # try the packages in another project (local feed)
cd samples/MinimalConsumer && dotnet test        # the smallest consumer
git tag archrules/v0.4.0 && git push origin archrules/v0.4.0   # release to NuGet.org — see RELEASING.md
```

Without a local SDK, the same through Docker (Podman works too; podman-compose needs `--profile tools` before `run`): `docker compose run --rm test`
runs the Debug build and the tests with a cached package volume, `docker compose run --rm catalog`
renders `rules.json`/`RULES.md`, and `docker build .` is the CI-style gate — the image only builds
when build, tests and catalog succeed and then carries the `.nupkg` files and the catalog under `/out`.

## Author

**Christoph Bloemer** — [@chbloemer](https://github.com/chbloemer)

## License

MIT — see [LICENSE](LICENSE).

Contributions are accepted under the MIT licence, and the copyright holder may additionally publish
them under other licences (for example a documentation licence for prose).
