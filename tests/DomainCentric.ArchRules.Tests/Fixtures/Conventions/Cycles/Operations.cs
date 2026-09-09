namespace DomainCentric.ArchRules.Tests.Fixtures.Conventions.Cycles.Ordering.Application.UseCases.Open {
 public class Open : DomainCentric.BuildingBlocks.Hexagonal.Ports.In.IInputPort { public Close.Close? Other { get; } }
}
namespace DomainCentric.ArchRules.Tests.Fixtures.Conventions.Cycles.Ordering.Application.UseCases.Close {
 public class Close : DomainCentric.BuildingBlocks.Hexagonal.Ports.In.IInputPort { public Open.Open? Other { get; } }
}
