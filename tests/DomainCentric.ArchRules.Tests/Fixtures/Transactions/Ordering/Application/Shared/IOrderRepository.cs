using DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Transactions.Ordering.Application.Shared;

public interface IOrderRepository : IRepository<Order, OrderId>
{
}
