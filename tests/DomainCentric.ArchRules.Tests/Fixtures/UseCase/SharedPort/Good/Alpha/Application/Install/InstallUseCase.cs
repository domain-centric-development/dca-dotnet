using DomainCentric.ArchRules.Tests.Fixtures.UseCase.SharedPort.Good.Alpha.Application.Shared;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.SharedPort.Good.Alpha.Application.Install;

public sealed class InstallUseCase(ISource source)
{
    public string Run() => source.Read();
}
