using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.SharedPort.Good.Alpha.Application.Shared;

/// <summary>Used by two use cases, declared in Application.Shared.</summary>
public interface ISource : IOutputPort
{
    string Read();
}
