using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.Shared;

public interface IOrderRepository : IRepository<Order, OrderId>
{
}
