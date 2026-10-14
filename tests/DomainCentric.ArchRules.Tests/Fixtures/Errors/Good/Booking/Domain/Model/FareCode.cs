using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Domain.Model;

/// <summary>A code, typed: what it holds is a value of the model, not text for a page.</summary>
public sealed record FareCode(string Value) : IValue;
