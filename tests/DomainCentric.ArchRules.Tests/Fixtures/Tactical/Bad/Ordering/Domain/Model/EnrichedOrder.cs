namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Domain.Model;

/// <summary>DCA-TAC-022: a record, but not an IValue.</summary>
public sealed record EnrichedOrder(OrderId OrderId, int LineItemCount);
