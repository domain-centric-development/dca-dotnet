using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Domain.Model;

/// <summary>A read model: a value the domain hands out instead of the aggregate — allowed in a result (DCA-USE-015).</summary>
public sealed record OrderSummary(OrderId Id, int LineCount) : IValue;

public sealed record Money(decimal Amount, string Currency) : IValue;
