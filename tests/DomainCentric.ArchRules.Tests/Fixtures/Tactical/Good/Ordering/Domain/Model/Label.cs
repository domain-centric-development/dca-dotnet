using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Domain.Model;

/// <summary>DCA-TAC-024: one named factory per origin - equal parameters, no neutral factory beside them.</summary>
public sealed record Label(string Text) : IValue
{
    public static Label FromInput(string text) => new(text.Trim());

    public static Label FromStored(string text) => new(text);
}
