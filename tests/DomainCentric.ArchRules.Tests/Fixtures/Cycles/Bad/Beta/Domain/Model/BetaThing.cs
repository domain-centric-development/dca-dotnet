using DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Alpha.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Beta.Domain.Model;

public sealed record BetaThing(string Id, AlphaThing? Related);
