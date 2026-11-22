using DomainCentric.ArchRules.Tests.Fixtures.UseCase.SharedPort.Good.Alpha.Application.Shared;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.SharedPort.Good.Alpha.Application.Check;

public sealed class CheckUseCase(ISource source)
{
    public string Run() => source.Read();
}
