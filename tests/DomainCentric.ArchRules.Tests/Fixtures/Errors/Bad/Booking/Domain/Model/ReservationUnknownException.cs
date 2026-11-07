using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Domain.Model;

/// <summary>Declared in the domain, where it belongs by its base type - but only a use case can raise it.</summary>
public sealed class ReservationUnknownException : DomainException
{
    public ReservationUnknownException(ReservationId id)
        : base($"reservation {id.Value} does not exist")
    {
    }
}
