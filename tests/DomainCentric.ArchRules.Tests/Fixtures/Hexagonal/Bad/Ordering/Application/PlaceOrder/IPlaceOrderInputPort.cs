using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Application.PlaceOrder;

public interface IPlaceOrderInputPort : IUseCase<PlaceOrderCommand, PlaceOrderResult>
{
}
