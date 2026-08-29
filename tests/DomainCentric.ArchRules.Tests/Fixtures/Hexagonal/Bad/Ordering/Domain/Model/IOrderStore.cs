using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Domain.Model;

/// <summary>DCA-HEX-010: an output port declared in the domain layer.</summary>
public interface IOrderStore : IStore
{
    void Put(Order order);
}
