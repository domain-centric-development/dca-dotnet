using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Ordering.Application.Shared;

public interface IOrderRepository : IRepository<Order, OrderId>
{
}
