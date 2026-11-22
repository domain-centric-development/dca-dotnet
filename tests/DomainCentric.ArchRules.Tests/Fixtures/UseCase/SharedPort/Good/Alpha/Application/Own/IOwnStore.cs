using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.SharedPort.Good.Alpha.Application.Own;

/// <summary>Used by its own use case only, declared beside it.</summary>
public interface IOwnStore : IOutputPort
{
    string Read();
}
