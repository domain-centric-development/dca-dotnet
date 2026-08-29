using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Application.Shared;

public interface IOrderAuditStore : IStore
{
    Task RecordAsync(OrderId orderId, string message, CancellationToken cancellationToken = default);

    Task<long> CountAsync(OrderId orderId, CancellationToken cancellationToken = default);
}
