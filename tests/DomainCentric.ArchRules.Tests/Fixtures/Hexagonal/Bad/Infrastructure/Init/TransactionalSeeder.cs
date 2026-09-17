using System.Data;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Application.PlaceOrder;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Infrastructure.Init;

/// <summary>
/// DCA-LAY-004: infrastructure may wire the transaction manager, but running a transaction through a transaction
/// API (TransactionApiTypes) draws a boundary - that belongs to the use case.
/// </summary>
public sealed class TransactionalSeeder
{
    private readonly IPlaceOrderInputPort _placeOrder;
    private readonly IDbTransaction _transaction;

    public TransactionalSeeder(IPlaceOrderInputPort placeOrder, IDbTransaction transaction)
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
