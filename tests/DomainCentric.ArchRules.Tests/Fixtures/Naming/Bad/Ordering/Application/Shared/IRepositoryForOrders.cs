using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Application.Shared;

// DCA-NAM-004: repository interface not ending with 'Repository'
public interface IRepositoryForOrders : IRepository<Order, OrderId>
{
}
