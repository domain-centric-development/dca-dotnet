namespace DomainCentric.ArchRules.Tests.Fixtures.Primitives.Good.Ledger.Domain.Model;

/// <summary>DCA-TAC-025: a two-member enum whose members have names of their own is no bool in disguise.</summary>
public enum AccountStatus
{
    Open,
    Closed,
}
