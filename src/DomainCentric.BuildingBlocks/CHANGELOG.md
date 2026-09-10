# Changelog — DomainCentric.BuildingBlocks

All notable changes to this package. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning: SemVer.

## [Unreleased]

## [0.1.1] - 2026-09-10

Documentation-only release: no type, signature or behaviour changed; the XML docs that ship in the package are what the
knowledge catalog renders. Binary-compatible with 0.1.0.

### Fixed

- The build no longer references `Microsoft.SourceLink.GitHub` 8.0.0: the SDK carries Source Link since 8.0.100. That
  package pulled `Microsoft.Build.Tasks.Git` 8.0.0, which NuGet audit reports as a moderate vulnerability
  (GHSA-23fw-v26w-5fgq); with warnings as errors it failed `dotnet restore` in CI. The reference was conditional on
  `GITHUB_ACTIONS`, so the failure was invisible locally. The packages keep their repository URL, commit and symbols.

### Changed

- Documentation only (WP-30): `ITransactionBoundary` names a `DbContext` transaction and `TransactionScope` as
  examples of an implementation instead of "EF Core: …"; `IDomainEventPublisher` says "Sequence of operations". The
  XML docs flow into the knowledge catalog and are now guarded against framework and shop vocabulary by
  `FrameworkNeutralityTests` in the ArchRules test project.

## [0.1.0] - 2026-09-07

Feature parity with `dca-building-blocks` 0.1.0.

### Added
- `Application.Transactions.ITransactionBoundary` — explicit transaction boundary inside a use case; an application-layer execution abstraction implemented by infrastructure, deliberately not an output port (remote calls stay outside the transaction).
- Initial .NET port of `dca-building-blocks` (Java): tactical markers, strategic attributes with marker-class convention, async-only hexagonal ports. Non-generic base interfaces `IEntity`, `IAggregateRoot`, `IRepository`.
