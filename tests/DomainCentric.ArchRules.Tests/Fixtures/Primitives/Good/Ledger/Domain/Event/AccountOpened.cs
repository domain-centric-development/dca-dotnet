using System;
using DomainCentric.ArchRules.Tests.Fixtures.Primitives.Good.Ledger.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Primitives.Good.Ledger.Domain.Event;

public sealed record AccountOpened(Guid EventId, AccountId Account, Holder Holder, DateTimeOffset OccurredOn) : IDomainEvent;
