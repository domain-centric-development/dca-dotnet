using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Primitives.Good.Ledger.Domain.Model;

public sealed record Holder(string Value) : IValue;
