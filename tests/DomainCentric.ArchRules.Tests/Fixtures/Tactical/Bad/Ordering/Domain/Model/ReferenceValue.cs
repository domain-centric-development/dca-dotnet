using System;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Domain.Model;
public interface IOrderReference : IAggregateRoot {}
public sealed record ReferenceValue(IOrderReference Order) : IValue;
