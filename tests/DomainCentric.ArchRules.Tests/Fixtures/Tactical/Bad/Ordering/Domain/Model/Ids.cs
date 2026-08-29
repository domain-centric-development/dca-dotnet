using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Domain.Model;

public readonly record struct OrderId(string Value) : IId, IValue;

public readonly record struct CustomerId(string Value) : IId, IValue;

public readonly record struct ShipmentId(string Value) : IId, IValue;
