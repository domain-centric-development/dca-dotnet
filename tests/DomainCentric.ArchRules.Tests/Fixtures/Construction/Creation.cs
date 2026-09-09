using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.ArchRules.Tests.Fixtures.Construction.Alpha.Domain;
namespace DomainCentric.ArchRules.Tests.Fixtures.Construction.Alpha.Domain {
 public record Line(string Id) : IEntity;
 public class LineFactory : IFactory { public static Line Reconstitute() => new Line("x"); }
 public readonly record struct RootId(string Value) : IId;
 public class OtherAggregate : AggregateRootBase<OtherAggregate, RootId> {
  public override RootId Id => new("other"); public Line Create() => new("x");
 }
}
namespace DomainCentric.ArchRules.Tests.Fixtures.Construction.Alpha.Application.Create {
 public class CreationUseCase { public object Execute() => new Line("x"); }
}
namespace DomainCentric.ArchRules.Tests.Fixtures.Construction.Alpha.Adapter.Outgoing {
 public class Persistence { public object Load() => LineFactory.Reconstitute(); public object Bypass() => new Line("x"); }
}
namespace DomainCentric.ArchRules.Tests.Fixtures.Construction.Beta.Domain {
 public class ForeignFactory : IFactory { public object Create() => new Line("x"); }
}
