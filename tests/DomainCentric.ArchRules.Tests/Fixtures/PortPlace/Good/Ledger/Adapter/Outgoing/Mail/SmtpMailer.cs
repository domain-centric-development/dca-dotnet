using DomainCentric.ArchRules.Tests.Fixtures.PortPlace.Good.Ledger.Application.Shared;

namespace DomainCentric.ArchRules.Tests.Fixtures.PortPlace.Good.Ledger.Adapter.Outgoing.Mail;

public sealed class SmtpMailer : IMailer
{
    public void Send(string to, string text)
    {
    }
}
