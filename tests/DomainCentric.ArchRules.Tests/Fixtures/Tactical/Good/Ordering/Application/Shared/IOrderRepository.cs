using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Application.Shared;

public interface IOrderRepository : IRepository<Order, OrderId>
{
    Task<IReadOnlyList<Order>> FindAllAsync(CancellationToken cancellationToken = default);

    Task<Order?> FindLatestAsync(CancellationToken cancellationToken = default);

    Task<long> CountAsync(CancellationToken cancellationToken = default);
}
