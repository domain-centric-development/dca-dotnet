namespace DomainCentric.ArchRules.Tests.Fixtures.Primitives.Bad.Ledger.Domain.Model;

/// <summary>DCA-TAC-025: two members that name one fact and its negation - a bool in disguise.</summary>
public enum Presence
{
    Present,
    Absent,
}
