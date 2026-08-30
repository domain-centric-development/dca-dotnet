# Changelog — DomainCentric.ArchRules (+ DomainCentric.ArchRules.Xunit)

All notable changes to these packages. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning: SemVer.

## [Unreleased]

### Changed
- `DCA-USE-012` (Java: publishing use cases must be transactional) listed as not applicable in `UseCaseRules.NotApplicable`; the catalog now reports 3 n/a rules.
- Target frameworks: `net8.0;net10.0` (BuildingBlocks additionally `netstandard2.1`); tests, tools and the
  minimal consumer sample build on `net10.0`.

### Fixed
- `DCA-CYC-001…004` sliced with ArchUnitNET's `Slices().Matching("Root.(*).Layer")`, which ignores the segments after `(*)` and slices every sub-namespace of a context — all four rules reported the same intra-context pairs (e.g. `Domain.Model` ↔ `Domain.Event`) as cycles. The rules now build one slice per context from the layer's types only and search elementary cycles between slices themselves (Java semantics of `Root.(*).layer..`). Found by the first real consumer (`dca-ecommerce-sample-dotnet`).

### Added
- Initial .NET port of `dca-archunit` (Java) on ArchUnitNET: same rule ids `DCA-<SET>-<NNN>`; `DcaLayout`, `DcaArchitecture`, `DcaRule`, `DcaRules`, `ContextMapRenderer`; xUnit base class `DcaArchitectureTest`.
- .NET-only rule set `dotnet` (`DCA-NET-001…`): synchronous domain, `Async` suffix on port methods, one `ExecuteAsync` per use case, records for values and ids.
