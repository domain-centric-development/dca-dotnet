using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Application.SetOrderStatus;

/// <summary>DCA-NAM-013: a use case named after the field it writes.</summary>
public interface ISetOrderStatusInputPort : IInputPort
{
}
