using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Primitives.Good.Ledger.Domain.Model;

/// <summary>A value of several components, each a value object.</summary>
public sealed record Address(Street Street, PostalCode PostalCode, City City) : IValue;
