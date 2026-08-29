using DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Beta.Adapter.Incoming;

namespace DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Alpha.Adapter.Incoming;

public sealed class AlphaController
{
    public string Handle(BetaController other) => other.Name();

    public string Name() => "Alpha";
}
