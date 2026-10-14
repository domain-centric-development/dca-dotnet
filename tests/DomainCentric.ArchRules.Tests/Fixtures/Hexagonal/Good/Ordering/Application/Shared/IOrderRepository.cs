using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Ordering.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Ordering.Application.Shared;

public interface IOrderRepository : IRepository<Order, OrderId>
{
}
