using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Domain.Model;

public readonly record struct OrderId(string Value) : IId, IValue;

/// <summary>Identity of an aggregate owned by another context — referenced by id only.</summary>
public readonly record struct CustomerId(string Value) : IId, IValue;

public readonly record struct LineItemId(string Value) : IId, IValue;
