using System.Data;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Application.PlaceOrder;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Adapter.Incoming.Bootstrap;

/// <summary>
/// DCA-LAY-004: an incoming adapter drawing the transaction boundary itself through a programmatic transaction
/// API (one of the configured TransactionApiTypes) instead of leaving it to the use case.
/// </summary>
public sealed class SeedRunner
{
    private readonly IPlaceOrderInputPort _placeOrder;
    private readonly IDbTransaction _transaction;

    public SeedRunner(IPlaceOrderInputPort placeOrder, IDbTransaction transaction)
    {
        _placeOrder = placeOrder;
        _transaction = transaction;
    }

    public void Run()
    {
        _ = _placeOrder;
        _transaction.Commit();
    }
}
