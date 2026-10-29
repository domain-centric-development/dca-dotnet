namespace DomainCentric.ArchRules.Tests.Fixtures.PortPlace.Bad.Ledger.Application.Shared;

/// <summary>An output port implemented beside its interface, in the application.</summary>
public sealed class ConsoleMailer : IMailer
{
    public void Send(string to, string text)
    {
    }
}
