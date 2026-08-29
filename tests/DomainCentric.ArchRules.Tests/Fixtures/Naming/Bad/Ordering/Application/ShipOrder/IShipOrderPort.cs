using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Application.ShipOrder;

// DCA-NAM-003: input port not named I*InputPort
public interface IShipOrderPort : IUseCase<string, string>
{
}
