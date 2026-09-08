# Changelog — DomainCentric.ArchRules (+ DomainCentric.ArchRules.Xunit)

All notable changes to these packages. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning: SemVer.

## [Unreleased]

### Added

- Every rule describes its mechanics: `IDcaRule.Selects` names the types the rule looks at, `IDcaRule.Checks`
  what it asserts about them - including what does not count and what is deliberately not established. Twin of
  `dca-archunit`'s `selects()`/`checks()`, same ids, same wording wherever the .NET reading is the same;
  `rules.json` carries both as `selects`/`checks`, `RULES.md` shows them as two columns.

- `DcaLayout.WithControllerSuffix(string)` (default `Controller`): the suffix of MVC controllers and page models is
  configurable like the REST one. `DCA-NAM-005` checks it, `DCA-HEX-003` selects controllers by either suffix (plus
  base class and `[ApiController]`). Previously `Controller` was hard-coded in both rules.
- `DCA-USE-009`'s rationale states what its check always did: only `PublishAndClearEventsAsync` counts as a
  publication; `PublishAsync(event)` per event, even followed by `ClearDomainEvents()`, is reported. A fixture pins it.

### Fixed

- Twelve rules still selected through the layout's one-segment wildcard patterns (`Root.[^.]+.Domain.Model` and
  siblings) and therefore saw only modules that are direct children of the root namespace: `DCA-TAC-001`,
  `DCA-TAC-009`, `DCA-TAC-013`, `DCA-TAC-014`, `DCA-TAC-015`, `DCA-TAC-019`, `DCA-TAC-020`, `DCA-TAC-022`,
  `DCA-NAM-007`, `DCA-NAM-008`, `DCA-NAM-010`, `DCA-USE-008`. They now select over `DcaArchitecture.ModuleRoots()`
  (`AllDomainModelPatterns()` etc.) like every other rule, so a module two segments deep (`Acme.Shop.Sales.Order`)
  is governed. A flat layout sees no difference. `DCA-ONI-003` and `DCA-TAC-001/009` no longer add the whole
  `SharedKernel.Domain` namespace on top - the shared kernel is a module root when it owns a domain layer, and
  `Domain.Model` is what the Java twin checks.
- `DCA-STR-002` evaluated one fluent rule per bounded context and threw at the first context with violations;
  it now collects all contexts (`EvaluateAll`) and reports them together, as the Java twin does.
- Parity with `dca-archunit` (each a real difference in what was reported, not wording): `DCA-TAC-022` selects
  interfaces named `Enriched*` too (reported, since an interface is no record); `DCA-ONI-002` no longer lets the
  domain depend on the strategic annotations or the input ports just because the default allow-list names the
  whole `DomainCentric.BuildingBlocks` namespace - of the building blocks only `Ddd.Tactical` and
  `Hexagonal.Ports.Out` are allowed; `DCA-NAM-001` selects only classes that implement `IUseCase<,>`, not every
  class assignable to `IInputPort`; `DCA-USE-015` walks non-public members as well and skips nested classes;
  `DCA-STR-007` no longer selects interfaces that extend `IIntegrationEvent`; `DCA-STR-008` checks `record struct`
  events (previously never selected because structs are no `Class` in the model).

### Changed

- **Rule authors:** `DcaRule.Of(...)` and `DcaRule.Check(...)` return `DcaRule.Undescribed`; the rule is
  completed with `.Selecting(string).Checking(string)`, both mandatory (blank text throws). Consumers that only
  run the catalog (`DcaArchitectureTest`, `DcaRules.CheckAll`) are unaffected.

## [0.2.0] - 2026-09-07

**Migrating from 0.1.0.** Four things can break a consumer; everything else is stricter enforcement
of the same rules and new rules that a 0.1.0 code base may fail.

1. `dca.rule.<id>.ignore` holds **one** regular expression. Several expressions move to indexed keys
   (`.ignore.1`, `.ignore.2`, …) or are joined with `|`.
2. A context is identified by its namespace **relative to the root namespace** (`Sales.Order`, not
   `Order`). `[Upstream("…")]`, `[Partnership("…")]` and ignore patterns naming a nested context change
   accordingly; contexts that are direct children of the root are unaffected. `SimpleContextName` is
   obsolete.
3. Contexts are discovered by `[BoundedContext]` at any depth and the rules select over discovered
   modules — a context that the one-segment wildcard used to skip is governed now.
4. Four new rules (`DCA-USE-014`, `DCA-USE-015`, `DCA-HEX-012`, `DCA-CYC-005`) and the tightened
   `DCA-TAC-003/-007/-008`, `DCA-USE-009`, `DCA-LAY-003`, `DCA-HEX-004/-005`, `DCA-MAP-001` and
   `DCA-STR-003/-004/-006` can report code that passed 0.1.0. Lower a rule to `Warning(...)` while you
   fix it — `dca-archunit.properties` documents the reason.

Before 1.0 a minor version may tighten rules; each such change is listed under *Changed — breaking*.

### Added
- **Shaping the result — two rules** (ported from `dca-archunit`, same ids, titles and rationales). A use case
  result carries the answer, never a handle on the model; an incoming adapter formats that answer and obtains
  no domain collaborator of its own. Outgoing adapters are deliberately outside both rules — repositories and
  persistence mappers construct and reconstitute domain objects while implementing output ports.
  - `DCA-USE-015` — use case result models must not expose aggregate roots or entities. Selects every
    `*Result` class in an application namespace that is not an `IValue`, resolves its runtime type and walks
    its public properties and fields **transitively**: through generic type arguments (`IReadOnlyList<T>`,
    `IReadOnlyDictionary<K,V>`), `Nullable<T>`, arrays, records nested in the result and part records — record
    classes and record structs — anywhere in the application layer, `Application.Shared` included (parts carry
    no `Result` suffix); a generic part record is walked through its type arguments and its own members. Every member assignable to `IAggregateRoot` or `IEntity`
    is reported with its path (`ListOrdersResult.Latest -> OrderView.Order : Order (IAggregateRoot)`).
  - `DCA-HEX-012` — incoming adapters must not depend on domain services. Every class in an incoming adapter
    namespace (event consumers included) is checked for dependencies on a type assignable to
    `IDomainService`, through the ArchUnitNET dependency model and the runtime type's constructor
    parameters. The mechanical subset of "an incoming adapter derives no business facts"; construction of
    domain objects and calls into domain behaviour stay review checks, because a result may legitimately
    carry a domain `IValue` or read model the adapter has to name.
  Catalog: 116 rules (110 ported + 6 .NET-only, 4 Java rules n/a).
- **Features within a bounded context — two rules and a compatibility fixture** (ported from `dca-archunit`,
  same ids, titles and rationales). A *feature* is an optional, domain-named group of related use cases below
  a module's application namespace (`Application.<Feature>.<UseCase>`, e.g.
  `Checkout.Application.Session.StartCheckout`) — a navigation and cohesion boundary inside one bounded
  context, not a layer, module, aggregate owner or deployment unit; nothing in the library infers bounded
  contexts or aggregate ownership from it. The pre-existing rules already saw such namespaces (they select
  "below the layer"); `Fixtures/Features/CompatFixture.cs` runs the whole catalog against a grouped context
  so a later change of a selector into a direct-child assumption fails there first.
  - `DCA-USE-014` — use case namespaces within a module must use one consistent depth: flat
    (`Application.<UseCase>`) or grouped (`Application.<Feature>.<UseCase>`). Selects the classes ending in
    the configured `UseCaseSuffix`, ignores `Application.Shared`, abstract classes and nested types (async state
    machines included), and reports every offending module and namespace in one violation. A module without use
    cases is valid; a single use case may use either depth. Legibility only.
  - `DCA-CYC-005` — the immediate child namespaces of a module's application namespace (`Shared` excepted)
    must be free of cycles: features in a grouped layout, use cases in a flat one. Uses the same
    deterministic slice graph as `DCA-CYC-001..004`, with slices assigned from `ModuleRootOf(...)` and the
    first namespace segment below `Application`.
  Catalog: 114 rules (108 ported + 6 .NET-only, 4 Java rules n/a).
- **Context discovery at any depth.** `DcaArchitecture.RootContextNamespace` walks up from a type's
  namespace to the nearest ancestor whose marker class carries `[BoundedContext]` or `[SharedKernel]`,
  so a context may be grouped (`Acme.Shop.Sales.Order`) or be the root namespace itself. A context is
  identified by its namespace relative to the root (`ContextName`: `Sales.Order`), which is what the
  context-map rules, the renderer and rule messages now use; `SimpleContextName` is obsolete. For a
  context that is a direct child of the root the identifier is unchanged.
- **`DcaArchitecture.ModuleRoots()` — structural module discovery.** A module root is the shortest
  namespace prefix whose remainder starts with a layer segment (`Domain`, `Application`, `Adapter`),
  found at any depth and without an attribute. Distinct from `BoundedContexts`: being a context is a
  strategic declaration, owning a layer is a structural fact, and the layer rules apply to both.
  Plus `ModuleRootOf`, `LayerSegments`, `IsolatedModuleRoots` (module roots minus the shared kernel),
  `ModuleRootPatternsExcluding`, `PublishedPatternsExcluding`, and `AllDomain/DomainModel/Application/
  SharedOutputPort/Adapter/IncomingAdapter/OutgoingAdapterPatterns()` over module roots.
  `AllDomainPatternsWithSharedKernel` / `AllDomainModelPatternsWithSharedKernel` are obsolete aliases.
- **`DcaLayout.WithApiSegment()` / `WithEventsSegment()`** — the published-contract segments are layout
  settings like every other segment (defaults `Api`, `Events`; the two must differ), read via
  `ApiSegment`, `EventsSegment`, `PublishedSegments`. `DCA-STR-005/006/007`, the context-map rules and
  `ContextMapRenderer` take them from the layout; nothing hard-codes the channel names any more.
  Also `DcaLayout.AnyOf(patterns)` (alternation; empty → matches nothing) and `AnySegmentPath`.
- `DcaRule.EvaluateAll(rules, arch, title, rationale)` — evaluates several fluent rules that make up one
  DCA rule and throws once with all their violations.

### Changed — breaking
- **An `.ignore` property value is one regular expression.** `dca.rule.<id>.ignore` was split on commas
  like a list of rule ids, so `Foo.{1,3}Bar` failed as an invalid expression. The value is now taken as
  written; a second expression for the same rule uses an indexed key (`.ignore.1`, `.ignore.2`, …, applied
  after the unindexed one). Same semantics as `dca-archunit`.
- **`DCA-USE-009` checks the entry path, not the class.** The rule asked only that a `SaveAsync` and a
  `PublishAndClearEventsAsync` call exist somewhere in the use case class, so a publication in an unrelated
  method covered a saving method. It now follows the directed calls within the class (`IntraClassCalls`,
  built from the runtime type's IL — the compiler's async state machines and lambda closures are units of
  the graph, joined to the method that declares them): for every unit that calls `SaveAsync`, every entry
  point reaching it must also reach a `PublishAndClearEventsAsync`. An entry point is a unit callable from
  outside the class — any non-private method or constructor written in source, so a public method stays an
  entry point even when another method of the class also calls it (compiler-generated units are not) — or
  one nothing in the class calls.
  An entry method may save through one helper and publish through another, over any number of steps; a
  helper two entry methods share does not connect them; a saving helper shared by a publishing and a
  non-publishing entry method is reported for the latter (`Foo.ExecuteQuietlyAsync (via PersistAsync)`); a
  public `ExecuteAsync` that only saves is reported even when a public `CompleteAsync` calls it and publishes
  afterwards; recursion terminates. Rationale identical to the Java rule, which reasons the same way; order of the two
  calls and the identity of the aggregate stay outside the check. Fixtures: `Fixtures/Transactions`.
- **`DCA-TAC-003`, `-007`, `-008` see arrays and generic base classes.** The member walk took the generic
  arguments from the ArchUnitNET model only, so `Order[]` in an aggregate, entity or value object passed, and a
  member inherited from a generic base (`class Base<T> { T Value; }`, `Order : Base<Customer>`) was an open
  type parameter. `DataMembers` now adds what reflection sees in the inspected type's context — array element
  types, generic arguments recursively, the concrete type a base's parameter is bound to. `DCA-TAC-003` keeps
  its one tolerance exactly: a direct member of the own type passes; a container of the own type
  (`IReadOnlyList<Category>`, `Category[]`, `IReadOnlyDictionary<string, Category>`, nested lists) holds
  *other* instances and is reported — as the Java rule does.

### Changed
- **Rules select over discovered modules, not over a one-segment wildcard.** Every rule that used
  `Layout.DomainPattern` and its siblings (`Root.[^.]+.Domain`, exactly one segment) now selects through
  `arch.All*Patterns()` built from `ModuleRoots()`. A context grouped or nested one level too deep used
  to match no rule and pass silently; it is governed now. The wildcard properties remain for tooling.
- **Isolation is structural.** `DCA-STR-003`, `DCA-STR-004`, `DCA-STR-006` and `DCA-HEX-007` iterate over
  `IsolatedModuleRoots()` on the source and the target side, so a module that declares no
  `[BoundedContext]` can neither reach into a neighbour's internals nor have its own reached into. The
  adapter allow-list is the target's `Api`/`Events` namespaces; `DCA-STR-006` forbids everything else in
  a foreign module (adapters and infrastructure included). Titles and rationales say "module"; the four
  rules report every offending module, not only the first. Same ids and texts as `dca-archunit`.
- **`DCA-STR-005`** accepts an Open Host Service in `Api` or anywhere under `Adapter.Incoming` — the
  pattern is the published relationship, not a folder; the former `Adapter.Incoming.OpenHost` requirement
  is gone.
- **Cycle rules slice by module root.** `CycleRules.CheckSlices` assigned slices with a one-segment
  regex capture, so two contexts grouped below an intermediate namespace produced no slices and a cycle
  between them went unreported. Slices are now `ModuleRootOf(namespace)`, at any depth.

### Fixed
- **`DCA-MAP-001` sees declarations on nested namespaces.** A `[Partnership]` on a marker class in
  `Cart.Application.GetCart` was neither reported nor rendered; the rule inspected the resolved context
  roots only. It now inspects every namespace below the root that a loaded type lives in, ancestors
  included (`DcaArchitecture.NamespacesBelowRoot()`), and requires the declaring namespace itself to carry
  `[BoundedContext]`.
- **`DCA-USE-015` includes inherited members and reports every path.** A `sealed class ArchivedOrdersResult
  : BaseListing` inherited an aggregate property from a base class without the `Result` suffix and passed
  (`BindingFlags.DeclaredOnly`); and a global visited set reported only the first path through a part
  record. The walker now reads all public instance members and keeps only the records on the current path,
  so `FirstLine -> LineView.Line` and `LastLine -> LineView.Line` are both reported.
- **`DCA-NAM-011` works for grouped and single-context layouts.** The rule built
  `Root.<segment>.Adapter.Incoming.Web`; the allowed namespaces are now derived from the module roots.
- **Infrastructure is selected at both levels.** `IsInfrastructureImplementation` looked at the global
  `Root.Infrastructure` only. `DcaArchitecture.InfrastructureNamespaces()` lists it plus every isolated
  module's own `Infrastructure` namespace (the shared kernel's excluded — shared support, not one module's
  detail); `DCA-LAY-002`, `-003`, `DCA-HEX-004` and `-005` use it.

## [0.1.0] - 2026-09-07

Feature parity with `dca-archunit` 0.1.0 (same rule ids and rationales; 4 Java rules not applicable, 6 .NET-only rules).

### Added
- Initial .NET port of `dca-archunit` (Java) on ArchUnitNET: same rule ids `DCA-<SET>-<NNN>`; `DcaLayout`, `DcaArchitecture`, `DcaRule`, `DcaRules`, `ContextMapRenderer`; xUnit base class `DcaArchitectureTest`.
- .NET-only rule set `dotnet` (`DCA-NET-001…`): synchronous domain, `Async` suffix on port methods, one `ExecuteAsync` per use case, records for values and ids.
- **Configurable rule selection.** `DcaRuleSelection` decides which rules run and how strictly:
  `OnlySets` / `OnlyIds` narrow the run, `Excluding(id, reason)` switches a rule off,
  `Warning(id, reason)` reports it without failing the build, and
  `IgnoringViolationsMatching(id, regex)` tolerates a documented exception. Readable from
  `dca-archunit.properties` next to the test assembly (same keys as the Java library), which the xUnit
  base class reads and merges `AdditionalSelection` on top of — override that property, not
  `Selection`, which would replace the file. Unknown rule ids and set names fail the run.
  No baseline dial: ArchUnitNET has no `FreezingArchRule`, and `dca.rules.freeze*` is rejected with a
  message pointing at `dca.rules.warn`.
- **New public API:** `DcaSeverity`, `DcaRuleSelection`, `DcaRuleExecution`, `DcaRuleOutcome`,
  `DcaRules.Select/SelectFlat/SetNames/SetOfRule`, `DcaRules.CheckAll(architecture, selection)`, plus
  `Header`/`Violations`/`Retaining` on `DcaRuleViolationException`. `ExcludedRuleIds` and `Rules` keep
  working.
- `DCA-HEX-011` — incoming adapters must depend on input port interfaces, not on use case classes (ported from `dca-archunit`, same id). Injecting the concrete implementation couples the adapter to one realisation and defeats the Dependency Inversion Principle the port exists for. Catalog: 112 rules (106 ported + 6 .NET-only, 4 Java rules n/a).
- `DCA-NET-006` — application layer must not use persistence or transaction frameworks (EF Core, System.Data, System.Transactions, Dapper, NHibernate, MongoDB driver); the boundary is a decorator or the `ITransactionBoundary` port.

### Changed
- Theory cases are named by rule set (`tactical / DCA-TAC-001`), a rule the properties file lowers to
  `WARN` or `OFF` carries its severity and recorded reason in the display name, and a rule the file
  scopes out produces no case at all — the test count now says what was actually checked. Previously
  an excluded rule passed silently, which hid the decision; xUnit v2 cannot skip dynamically, so a
  lowered rule is still reported green. Settings made in `AdditionalSelection` take effect but cannot
  shape the display name, because theory data is built before an instance exists.
- `DCA-USE-013` (Java: transactional use cases must not call remote-capable ports) listed as not applicable; 4 n/a rules now.
- `DCA-USE-012` (Java: publishing use cases must be transactional) listed as not applicable in `UseCaseRules.NotApplicable`; the catalog now reports 3 n/a rules.
- Target frameworks: `net8.0;net10.0` (BuildingBlocks additionally `netstandard2.1`); tests, tools and the
  minimal consumer sample build on `net10.0`.

### Fixed
- `DCA-LAY-005` ignores compiler-generated nested types (closures, async state machines of default interface methods) in `Ports.Out`.
- `DCA-CYC-001…004` sliced with ArchUnitNET's `Slices().Matching("Root.(*).Layer")`, which ignores the segments after `(*)` and slices every sub-namespace of a context — all four rules reported the same intra-context pairs (e.g. `Domain.Model` ↔ `Domain.Event`) as cycles. The rules now build one slice per context from the layer's types only and search elementary cycles between slices themselves (Java semantics of `Root.(*).layer..`). Found by the first real consumer (`dca-ecommerce-sample-dotnet`).
