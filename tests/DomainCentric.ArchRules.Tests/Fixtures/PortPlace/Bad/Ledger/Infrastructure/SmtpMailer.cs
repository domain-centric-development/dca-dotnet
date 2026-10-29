using DomainCentric.ArchRules.Tests.Fixtures.PortPlace.Bad.Ledger.Application.Shared;

namespace DomainCentric.ArchRules.Tests.Fixtures.PortPlace.Bad.Ledger.Infrastructure;

/// <summary>An output port implemented in the configuration namespace instead of an outgoing adapter.</summary>
public sealed class SmtpMailer : IMailer
{
    public void Send(string to, string text)
    {
    }
}
