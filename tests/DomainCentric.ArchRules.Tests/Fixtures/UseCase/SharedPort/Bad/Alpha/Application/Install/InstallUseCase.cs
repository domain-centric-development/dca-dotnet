namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.SharedPort.Bad.Alpha.Application.Install;

public sealed class InstallUseCase(ISource source)
{
    public string Run() => source.Read();
}
