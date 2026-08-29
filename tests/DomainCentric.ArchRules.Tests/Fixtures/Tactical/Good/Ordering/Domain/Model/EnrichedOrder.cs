using DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Domain.Model;

/// <summary>Read projection combining an order with its total.</summary>
public sealed record EnrichedOrder(OrderId OrderId, int LineItemCount, Money Total) : IValue;
