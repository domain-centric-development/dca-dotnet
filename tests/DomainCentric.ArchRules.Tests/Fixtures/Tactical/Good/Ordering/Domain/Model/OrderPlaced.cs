using System;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Domain.Model;

public sealed record OrderPlaced(Guid EventId, DateTimeOffset OccurredOn, OrderId OrderId) : IDomainEvent;
