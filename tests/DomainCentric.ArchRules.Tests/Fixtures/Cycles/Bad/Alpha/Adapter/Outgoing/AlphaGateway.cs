using DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Beta.Adapter.Outgoing;

namespace DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Alpha.Adapter.Outgoing;

public sealed class AlphaGateway
{
    public string Call(BetaGateway other) => other.Name();

    public string Name() => "Alpha";
}
