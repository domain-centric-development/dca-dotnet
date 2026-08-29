using System;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Domain.Model;

public sealed record OrderPlaced(Guid EventId, DateTimeOffset OccurredOn, OrderId OrderId) : IDomainEvent;
