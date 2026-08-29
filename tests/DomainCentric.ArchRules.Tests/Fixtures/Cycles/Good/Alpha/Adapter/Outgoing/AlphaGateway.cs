using DomainCentric.ArchRules.Tests.Fixtures.Cycles.Good.Alpha.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Cycles.Good.Alpha.Adapter.Outgoing;

public sealed class AlphaGateway
{
    public string Send(AlphaThing thing) => thing.Id;
}
