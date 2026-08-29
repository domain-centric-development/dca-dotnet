using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace Acme.Shop.Cart.Application.GetCart;

public interface IGetCartInputPort : IUseCase<GetCartQuery, GetCartResult>
{
}
