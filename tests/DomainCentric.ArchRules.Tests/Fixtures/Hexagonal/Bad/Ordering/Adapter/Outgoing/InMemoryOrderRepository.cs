using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Adapter.Outgoing;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<OrderId, Order> _orders = new();
    private readonly System.Func<IOrderRepository> _self = DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Infrastructure.Config.InMemoryConfig.OrderRepository; // DCA-HEX-005

    public Task<Order?> FindByIdAsync(OrderId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_orders.TryGetValue(id, out var order) ? order : null);

    public Task<Order> SaveAsync(Order aggregate, CancellationToken cancellationToken = default)
    {
        _orders[aggregate.Id] = aggregate;
        return Task.FromResult(aggregate);
    }

    public Task DeleteByIdAsync(OrderId id, CancellationToken cancellationToken = default)
    {
        _orders.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
