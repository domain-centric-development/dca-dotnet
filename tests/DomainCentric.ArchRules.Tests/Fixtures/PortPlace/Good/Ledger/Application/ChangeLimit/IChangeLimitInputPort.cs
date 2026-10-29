using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DomainCentric.ArchRules.Tests.Fixtures.PortPlace.Good.Ledger.Application.ChangeLimit;

public interface IChangeLimitInputPort : IInputPort
{
    void Change(string account, long amount);
}
