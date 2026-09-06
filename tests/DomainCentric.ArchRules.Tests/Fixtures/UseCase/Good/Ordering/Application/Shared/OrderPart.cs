using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Application.Shared;

/// <summary>A part record shared between several results: values only.</summary>
public sealed record OrderPart(string Sku, int Position, Money LineTotal);
