using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Adapter.Outgoing;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<OrderId, Order> _storage = new();

    public Task<Order?> FindByIdAsync(OrderId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_storage.TryGetValue(id, out var order) ? order : null);

    public Task<Order> SaveAsync(Order aggregate, CancellationToken cancellationToken = default)
    {
        _storage[aggregate.Id] = aggregate;
        return Task.FromResult(aggregate);
    }

    public Task DeleteByIdAsync(OrderId id, CancellationToken cancellationToken = default)
    {
        _storage.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Order>> FindAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Order>>(_storage.Values.ToList());

    public Task<Order?> FindLatestAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_storage.Values.FirstOrDefault());

    public Task<long> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult((long)_storage.Count);
}
