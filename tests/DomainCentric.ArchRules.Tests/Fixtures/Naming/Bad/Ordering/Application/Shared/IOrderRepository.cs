using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Application.Shared;

public interface IOrderRepository : IRepository<Order, OrderId>
{
}
