using System;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Ordering.Domain.Model;

public sealed record OrderPlaced(Guid EventId, DateTimeOffset OccurredOn, OrderId OrderId) : IDomainEvent;
