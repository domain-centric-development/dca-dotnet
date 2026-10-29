using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.PortPlace.Good.Ledger.Application.Shared;

public interface IMailer : IOutputPort
{
    void Send(string to, string text);
}
