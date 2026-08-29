using DomainCentric.ArchRules.Tests.Fixtures.Cycles.Good.Beta.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Cycles.Good.Beta.Adapter.Outgoing;

public sealed class BetaGateway
{
    public string Send(BetaThing thing) => thing.Id;
}
