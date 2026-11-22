using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.SharedPort.Bad.Alpha.Application.Install;

/// <summary>Declared in one use case, used by two: belongs in Application.Shared.</summary>
public interface ISource : IOutputPort
{
    string Read();
}
