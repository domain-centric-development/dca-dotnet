using System;
using DomainCentric.ArchRules.Tests.Fixtures.Primitives.Bad.Ledger.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Primitives.Bad.Ledger.Domain.Event;

/// <summary>OccurredOn is the event marker's contract and passes; the holder's name does not.</summary>
public sealed record AccountOpened(Guid EventId, AccountId Account, string Holder, DateTimeOffset OccurredOn) : IDomainEvent;
