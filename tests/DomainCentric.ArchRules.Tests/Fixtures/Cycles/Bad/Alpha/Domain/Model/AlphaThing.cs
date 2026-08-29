using DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Beta.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Alpha.Domain.Model;

public sealed record AlphaThing(string Id, BetaThing? Related);
