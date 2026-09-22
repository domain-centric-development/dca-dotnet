# Changelog — DomainCentric.BuildingBlocks

All notable changes to this package. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning: SemVer.

## [Unreleased]

## [0.2.1] - 2026-09-22

Documentation-only release: no type, signature or behaviour changed, binary-compatible with 0.2.0.
The XML docs that ship in the package are what the knowledge catalog renders, so a doctrine written
into a marker's contract reaches readers only once the package is published.

### Changed — documentation

- `IDomainGateway` states that it never writes to the outside — the twin of the `DomainGateway`
  change in `dca-building-blocks` 0.3.1, same wording. A gateway obtains facts the domain does not
  hold, or delegates a technology-bound computation, so that the model can decide; it changes the
  state of no external system. Persisting, sending, publishing and calling a remote operation that
  has an effect are the application's business, through an output port. A computation that touches
  nothing outside the process — hashing a password, converting a currency with a supplied rate — is
  a gateway, because no external state changes. Recorded as a decision on 2026-09-22.

## [0.2.0] - 2026-09-21

### Added

- `Ddd.Tactical.DomainException` and `Application.UseCaseException` — abstract, unchecked base classes for the two
  kinds of failure an inner layer raises: a broken business rule of the model and a request the use case cannot
  serve. Neither carries a code or status field; mapping an outcome onto a protocol answer is the incoming
  adapter's decision. `UseCaseException` rather than `ApplicationException` because the platform occupies that
  name and discourages deriving from it, and because one word should name the concept in both libraries. The
  `errors` rule set of `DomainCentric.ArchRules` (`DCA-ERR-001` … `DCA-ERR-006`) checks their use.

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
