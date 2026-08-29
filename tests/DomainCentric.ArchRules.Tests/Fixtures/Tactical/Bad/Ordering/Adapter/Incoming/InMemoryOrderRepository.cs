using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Application.PlaceOrder;
using DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Adapter.Incoming;

/// <summary>DCA-TAC-015: repository implementation outside Adapter.Outgoing.</summary>
public sealed class InMemoryOrderRepository : IOrderRepository
{
    public Task<Order?> FindByIdAsync(OrderId id, CancellationToken cancellationToken = default) => Task.FromResult<Order?>(null);

    public Task<Order> SaveAsync(Order aggregate, CancellationToken cancellationToken = default) => Task.FromResult(aggregate);

    public Task DeleteByIdAsync(OrderId id, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<Shipment?> FindShipmentAsync(OrderId id, CancellationToken cancellationToken = default) => Task.FromResult<Shipment?>(null);
}
