using DomainCentric.ArchRules.Tests.Fixtures.UseCase.SharedPort.Bad.Alpha.Application.Install;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.SharedPort.Bad.Alpha.Application.Check;

public sealed class CheckUseCase(ISource source)
{
    public string Run() => source.Read();
}
