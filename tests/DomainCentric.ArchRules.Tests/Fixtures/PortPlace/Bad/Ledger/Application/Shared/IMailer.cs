using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.PortPlace.Bad.Ledger.Application.Shared;

public interface IMailer : IOutputPort
{
    void Send(string to, string text);
}
