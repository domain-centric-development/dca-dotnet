namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.SharedPort.Good.Alpha.Application.Own;

public sealed class OwnUseCase(IOwnStore ownStore)
{
    public string Run() => ownStore.Read();
}
