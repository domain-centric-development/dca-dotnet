using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Listeners.Bad.Ledger.Application.Shared;

public interface IPostingMetrics : IOutputPort
{
    void Count(string reference);
}
