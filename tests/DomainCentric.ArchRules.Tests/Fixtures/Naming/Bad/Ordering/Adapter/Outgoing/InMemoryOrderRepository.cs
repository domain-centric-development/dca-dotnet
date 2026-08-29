using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Adapter.Outgoing;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<OrderId, Order> _store = new();

    public Task<Order?> FindByIdAsync(OrderId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id, out var order) ? order : null);

    public Task<Order> SaveAsync(Order aggregate, CancellationToken cancellationToken = default)
    {
        _store[aggregate.Id] = aggregate;
        return Task.FromResult(aggregate);
    }

    public Task DeleteByIdAsync(OrderId id, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
