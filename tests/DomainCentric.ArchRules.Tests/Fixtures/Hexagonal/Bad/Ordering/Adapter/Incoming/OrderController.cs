using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.AspNetCore.Mvc;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Infrastructure.Config;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Adapter.Outgoing;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Domain.Model;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.SharedKernel.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Adapter.Incoming;

// Violations sit in synchronous members on purpose: ArchUnitNET attributes the body of an async method
// to the compiler-generated state machine, not to the declaring type.
[ApiController]
public sealed class OrderController : ControllerBase
{
    private readonly IOrderRepository _orders = new InMemoryOrderRepository(); // DCA-HEX-003 (repository) + DCA-HEX-006 (outgoing adapter)

    public Task<Order> Place(decimal amount, CancellationToken cancellationToken)
    {
        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled); // DCA-LAY-004: transaction in incoming adapter
        _ = InMemoryConfig.OrderRepository(); // DCA-HEX-004: incoming adapter -> infrastructure implementation
        var order = Order.Place(new Money(amount, "EUR"));
        scope.Complete();
        return _orders.SaveAsync(order, cancellationToken);
    }
}
