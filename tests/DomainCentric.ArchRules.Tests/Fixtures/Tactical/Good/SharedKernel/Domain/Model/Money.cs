using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.SharedKernel.Domain.Model;

public sealed record Money(decimal Amount, string Currency) : IValue;
