using DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Alpha.Adapter.Incoming;

namespace DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Beta.Adapter.Incoming;

public sealed class BetaController
{
    public string Handle(AlphaController other) => other.Name();

    public string Name() => "Beta";
}
