using System.Data;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Infrastructure.Config;

/// <summary>The composition root wires the transaction handle; it draws no boundary and is not a LAY-004 finding.</summary>
public static class TransactionConfig
{
    public static IDbTransaction? Transactions() => null;
}
