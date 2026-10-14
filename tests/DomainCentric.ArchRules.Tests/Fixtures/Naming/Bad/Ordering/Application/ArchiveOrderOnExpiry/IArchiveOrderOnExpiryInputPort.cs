using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Application.ArchiveOrderOnExpiry;

/// <summary>DCA-NAM-012: a use case named after its trigger, not its effect.</summary>
public interface IArchiveOrderOnExpiryInputPort : IInputPort
{
}
