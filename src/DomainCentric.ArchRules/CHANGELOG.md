# Changelog — DomainCentric.ArchRules (+ DomainCentric.ArchRules.Xunit)

All notable changes to these packages. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning: SemVer.

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
