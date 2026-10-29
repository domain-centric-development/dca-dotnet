using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.PortPlace.Good.Ledger.Application.Shared;

public interface ILimitSetter : IOutputPort
{
    void SetLimit(string account, long amount);
}
