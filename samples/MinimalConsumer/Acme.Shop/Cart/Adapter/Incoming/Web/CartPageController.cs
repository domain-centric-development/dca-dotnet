using Acme.Shop.Cart.Application.GetCart;
using Acme.Shop.Cart.Domain.Model;

namespace Acme.Shop.Cart.Adapter.Incoming.Web;

/// <summary>Framework-free stand-in for a web controller.</summary>
public sealed class CartPageController
{
    private readonly IGetCartInputPort _getCart;

    public CartPageController(IGetCartInputPort getCart)
    {
        _getCart = getCart;
    }

    public Task<GetCartResult> ShowAsync(Guid cartId, CancellationToken cancellationToken = default) =>
        _getCart.ExecuteAsync(new GetCartQuery(new CartId(cartId)), cancellationToken);
}
