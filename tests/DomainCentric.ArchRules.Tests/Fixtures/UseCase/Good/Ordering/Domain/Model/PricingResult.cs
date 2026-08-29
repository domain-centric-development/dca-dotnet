using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Domain.Model;

// A domain value object whose name ends with 'Result' is allowed in the domain (DCA-USE-006).
public sealed record PricingResult(decimal Total) : IValue;
