namespace Acme.Shop.Cart.Application.GetCart;

public sealed record GetCartResult(Guid CartId, IReadOnlyList<GetCartResult.Line> Lines)
{
    public sealed record Line(Guid ProductId, int Quantity);
}
