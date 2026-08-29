using DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Alpha.Adapter.Outgoing;

namespace DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Beta.Adapter.Outgoing;

public sealed class BetaGateway
{
    public string Call(AlphaGateway other) => other.Name();

    public string Name() => "Beta";
}
