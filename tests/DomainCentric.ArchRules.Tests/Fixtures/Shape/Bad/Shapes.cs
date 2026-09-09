using System;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
namespace DomainCentric.ArchRules.Tests.Fixtures.Shape.Bad.Ordering.Application.Submit {
 public sealed class StateCommand { public int Amount { get; set; } }
 public sealed class StateQuery { public int Amount { get; set; } }
 public sealed class StateResult { public int Amount { get; set; } }
 public sealed record RecordCommand { public int Amount { get; set; } }
}
namespace DomainCentric.ArchRules.Tests.Fixtures.Shape.Bad.Ordering.Domain {
 public sealed record RecordChanged : IDomainEvent, IIntegrationEvent {
   public int Amount { get; set; }
   public Guid EventId { get; init; }
   public DateTimeOffset OccurredOn { get; init; }
 }
 public struct StructChanged : IDomainEvent, IIntegrationEvent {
   public int Amount { get; set; }
   public Guid EventId { get; init; }
   public DateTimeOffset OccurredOn { get; init; }
 }
}
namespace DomainCentric.ArchRules.Tests.Fixtures.Shape.Bad.Ordering.Domain {
 public struct MutableValue : IValue { public int Amount; }
 public sealed record MutableRecordValue : IValue { public int Amount { get; set; } }
 public record struct MutableId(Guid Value) : IId;
}
namespace DomainCentric.ArchRules.Tests.Fixtures.Shape.Bad.Ordering.Application.Submit {
 public readonly record struct StructResult(DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Domain.Model.Shipment Entity);
}
