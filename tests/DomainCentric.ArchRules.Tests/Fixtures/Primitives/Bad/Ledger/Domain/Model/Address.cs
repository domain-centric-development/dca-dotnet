using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Primitives.Bad.Ledger.Domain.Model;

/// <summary>A value of several components: each one is a value too - the street and the city are not.</summary>
public sealed record Address(string Street, PostalCode PostalCode, string City) : IValue;
