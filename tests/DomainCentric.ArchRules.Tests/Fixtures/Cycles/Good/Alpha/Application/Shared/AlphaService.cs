using DomainCentric.ArchRules.Tests.Fixtures.Cycles.Good.Alpha.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Cycles.Good.Alpha.Application.Shared;

public sealed class AlphaService
{
    public AlphaThing Create(string id) => new(id);
}
