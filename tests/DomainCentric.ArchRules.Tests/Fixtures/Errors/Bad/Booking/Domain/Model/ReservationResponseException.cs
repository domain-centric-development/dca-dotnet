using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Domain.Model;

/// <summary>DCA-ERR-005: named after the answer instead of the broken rule.</summary>
public sealed class ReservationResponseException : DomainException
{
    public ReservationResponseException(ReservationId id)
        : base($"reservation {id.Value} cannot be served")
    {
    }
}
