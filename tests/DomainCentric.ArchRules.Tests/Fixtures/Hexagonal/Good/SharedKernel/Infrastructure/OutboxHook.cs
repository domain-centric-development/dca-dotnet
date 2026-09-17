using DomainCentric.BuildingBlocks.Application.Transactions;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.SharedKernel.Infrastructure;

/// <summary>Shared-kernel plumbing collaborating with the transaction boundary (an outbox hooking into its commit); it draws no boundary and is not a LAY-004 finding.</summary>
public sealed class OutboxHook
{
    private readonly ITransactionBoundary _transactions;

    public OutboxHook(ITransactionBoundary transactions)
    {
        _transactions = transactions;
    }

    public ITransactionBoundary Boundary => _transactions;
}
