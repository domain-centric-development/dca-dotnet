using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Domain.Model;

/// <summary>DCA-TAC-008: value object holding an entity.</summary>
public sealed record Address(string Street, Shipment Shipment) : IValue;
