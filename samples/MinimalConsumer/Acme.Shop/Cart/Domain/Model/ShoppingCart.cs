using Acme.Shop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace Acme.Shop.Cart.Domain.Model;

public sealed class ShoppingCart : AggregateRootBase<ShoppingCart, CartId>
{
    private readonly List<CartItem> _items = new();

    private ShoppingCart(CartId id)
    {
        Id = id;
    }

    public override CartId Id { get; }

    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();

    public static ShoppingCart Create() => new(CartId.Generate());

    public void AddItem(ProductId productId, Quantity quantity)
    {
        _items.Add(new CartItem(productId, quantity));
        RegisterEvent(new ItemAddedToCart(Guid.NewGuid(), DateTimeOffset.UtcNow, Id, productId));
    }
}
