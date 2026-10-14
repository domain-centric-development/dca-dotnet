using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Domain.Model;

/// <summary>DCA-TAC-024: a neutral factory beside the origin factory - both take one string.</summary>
public sealed record Code(string Text) : IValue
{
    public static Code Of(string text) => new(text);

    public static Code FromHash(string hash) => new(hash);
}
