using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Adapter.Outgoing;

public sealed class InMemoryOrderAuditStore : IOrderAuditStore
{
    private readonly ConcurrentDictionary<OrderId, long> _counts = new();

    public Task RecordAsync(OrderId orderId, string message, CancellationToken cancellationToken = default)
    {
        _counts.AddOrUpdate(orderId, 1L, (_, count) => count + 1);
        return Task.CompletedTask;
    }

    public Task<long> CountAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_counts.TryGetValue(orderId, out var count) ? count : 0L);
}
