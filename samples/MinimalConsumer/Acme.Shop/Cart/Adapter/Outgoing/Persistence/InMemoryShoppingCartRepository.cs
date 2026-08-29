using System.Collections.Concurrent;
using Acme.Shop.Cart.Application.Shared;
using Acme.Shop.Cart.Domain.Model;

namespace Acme.Shop.Cart.Adapter.Outgoing.Persistence;

public sealed class InMemoryShoppingCartRepository : IShoppingCartRepository
{
    private readonly ConcurrentDictionary<CartId, ShoppingCart> _store = new();

    public Task<ShoppingCart?> FindByIdAsync(CartId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id, out var cart) ? cart : null);

    public Task<ShoppingCart> SaveAsync(ShoppingCart aggregate, CancellationToken cancellationToken = default)
    {
        _store[aggregate.Id] = aggregate;
        return Task.FromResult(aggregate);
    }

    public Task DeleteByIdAsync(CartId id, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
