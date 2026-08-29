using Acme.Shop.Cart.Application.Shared;

namespace Acme.Shop.Cart.Application.GetCart;

public sealed class GetCartUseCase : IGetCartInputPort
{
    private readonly IShoppingCartRepository _carts;

    public GetCartUseCase(IShoppingCartRepository carts)
    {
        _carts = carts;
    }

    public async Task<GetCartResult> ExecuteAsync(GetCartQuery query, CancellationToken cancellationToken = default)
    {
        var cart = await _carts.FindByIdAsync(query.CartId, cancellationToken).ConfigureAwait(false)
                   ?? throw new ArgumentException("no cart", nameof(query));
        return new GetCartResult(
            cart.Id.Value,
            cart.Items.Select(i => new GetCartResult.Line(i.ProductId.Value, i.Quantity.Amount)).ToList());
    }
}
