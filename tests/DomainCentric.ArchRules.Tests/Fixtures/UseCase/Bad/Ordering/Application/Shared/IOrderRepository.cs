using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.Shared;

public interface IOrderRepository : IRepository<Order, OrderId>
{
}
