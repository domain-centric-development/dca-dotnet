using Acme.Shop.Cart.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace Acme.Shop.Cart.Application.Shared;

public interface IShoppingCartRepository : IRepository<ShoppingCart, CartId>
{
}
