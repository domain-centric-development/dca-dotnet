using System;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
namespace DomainCentric.ArchRules.Tests.Fixtures.Shape.Good.Ordering.Application.Submit {
 public sealed class StateCommand { public int Amount { get; init; } }
 public sealed class StateQuery { public int Amount { get; init; } }
 public sealed class StateResult { public int Amount { get; init; } }
 public sealed record RecordCommand { public int Amount { get; init; } }
}
namespace DomainCentric.ArchRules.Tests.Fixtures.Shape.Good.Ordering.Domain {
 public sealed record RecordChanged : IDomainEvent, IIntegrationEvent {
   public int Amount { get; init; }
   public Guid EventId { get; init; }
   public DateTimeOffset OccurredOn { get; init; }
 }
 public readonly struct StructChanged : IDomainEvent, IIntegrationEvent {
   public int Amount { get; init; }
   public Guid EventId { get; init; }
   public DateTimeOffset OccurredOn { get; init; }
 }
}
namespace DomainCentric.ArchRules.Tests.Fixtures.Shape.Good.Ordering.Domain {
 public sealed class ClassValue : IValue {
   public int Amount { get; }
   public ClassValue(int amount) { Amount = amount; }
   public override bool Equals(object? other) => other is ClassValue value && Amount == value.Amount;
   public override int GetHashCode() => Amount;
 }
 public readonly struct StructValue : IValue {
   public int Amount { get; }
   public StructValue(int amount) { Amount = amount; }
   public override bool Equals(object? other) => other is StructValue value && Amount == value.Amount;
   public override int GetHashCode() => Amount;
 }
 public readonly record struct RecordValue(int Amount) : IValue;
}
namespace DomainCentric.ArchRules.Tests.Fixtures.Shape.Good.Ordering.Application.Submit {
 public readonly record struct StructResult(decimal Total);
}
