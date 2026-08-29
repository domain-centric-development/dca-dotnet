using DomainCentric.ArchRules.Tests.Fixtures.Cycles.Good.Beta.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Cycles.Good.Beta.Application.Shared;

public sealed class BetaService
{
    public BetaThing Create(string id) => new(id);
}
