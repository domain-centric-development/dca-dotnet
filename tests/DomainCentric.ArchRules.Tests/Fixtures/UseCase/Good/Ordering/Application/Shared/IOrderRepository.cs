using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Application.Shared;

public interface IOrderRepository : IRepository<Order, OrderId>
{
}
