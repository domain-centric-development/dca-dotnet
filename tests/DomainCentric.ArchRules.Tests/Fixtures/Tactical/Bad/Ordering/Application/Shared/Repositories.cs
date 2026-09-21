using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Application.Shared;

/// <summary>DCA-TAC-013: named *Repository without extending the marker.</summary>
public interface ICustomerRepository
{
    Task<Customer?> FindByIdAsync(CustomerId id);
}

/// <summary>DCA-TAC-016: binds Order but is named after Phantom — must fail, not pass silently.</summary>
public interface IPhantomRepository : IRepository<Order, OrderId>
{
}

/// <summary>DCA-TAC-016: binds Order but is named after Shipment.</summary>
public interface IShipmentRepository : IRepository<Order, OrderId>
{
}

/// <summary>DCA-TAC-018: a *Store that extends IRepository instead of IStore.</summary>
public interface IShipmentStore : IRepository<Order, OrderId>
{
}
