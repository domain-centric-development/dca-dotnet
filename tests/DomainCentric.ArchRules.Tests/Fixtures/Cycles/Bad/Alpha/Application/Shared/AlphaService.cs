using DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Beta.Application.Shared;

namespace DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Alpha.Application.Shared;

public sealed class AlphaService
{
    public string Run(BetaService other) => other.Name();

    public string Name() => "Alpha";
}
