using DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Alpha.Application.Shared;

namespace DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Beta.Application.Shared;

public sealed class BetaService
{
    public string Run(AlphaService other) => other.Name();

    public string Name() => "Beta";
}
