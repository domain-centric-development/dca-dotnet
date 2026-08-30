# Changelog — DomainCentric.BuildingBlocks

All notable changes to this package. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning: SemVer.

## [Unreleased]

### Added
- `Hexagonal.Ports.Out.IUnitOfWork` — output port for an explicit transaction boundary inside a use case (remote calls stay outside the transaction).
- Initial .NET port of `dca-building-blocks` (Java): tactical markers, strategic attributes with marker-class convention, async-only hexagonal ports. Non-generic base interfaces `IEntity`, `IAggregateRoot`, `IRepository`.
