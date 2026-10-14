namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.Fulfilment.ShipOrderLater;

/// <summary>DCA-USE-018: the input port of the Fulfilment feature - qualified, because Application.IInputPort of this tree redeclares the base contract (DCA-USE-001).</summary>
public interface IShipOrderLaterInputPort : DomainCentric.BuildingBlocks.Hexagonal.Ports.In.IInputPort
{
}
