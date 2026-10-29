using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Primitives.Bad.Ledger.Domain.Model;

/// <summary>A wrapper value: its one component may be the primitive it wraps.</summary>
public sealed record PostalCode(string Value) : IValue
{
    public static PostalCode Of(string value) => new(value);
}
