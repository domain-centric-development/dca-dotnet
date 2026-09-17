using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Application.Transactions;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Domain.Service;

/// <summary>
/// DCA-LAY-004: a domain type depending on the ITransactionBoundary port - transactions are an application-layer
/// concern, the domain knows nothing of them.
/// </summary>
public sealed class TransactionalPricing
{
    private readonly ITransactionBoundary _transactions;

    public TransactionalPricing(ITransactionBoundary transactions)
    {
        _transactions = transactions;
    }

    public Task<decimal> Tax(decimal amount) => _transactions.InTransactionAsync(_ => Task.FromResult(amount / 5));
}
