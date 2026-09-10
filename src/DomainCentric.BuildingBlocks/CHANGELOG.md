# Changelog — DomainCentric.BuildingBlocks

All notable changes to this package. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning: SemVer.

## [Unreleased]

## [0.1.1] - 2026-09-10

Documentation-only release: no type, signature or behaviour changed; the XML docs that ship in the package are what the
knowledge catalog renders. Binary-compatible with 0.1.0.

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
