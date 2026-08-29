using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Application.PlaceOrder;

/// <summary>DCA-TAC-014 (not in Application.Shared), DCA-TAC-017 (returns a non-root entity).</summary>
public interface IOrderRepository : IRepository<Order, OrderId>
{
    Task<Shipment?> FindShipmentAsync(OrderId id, CancellationToken cancellationToken = default);
}
