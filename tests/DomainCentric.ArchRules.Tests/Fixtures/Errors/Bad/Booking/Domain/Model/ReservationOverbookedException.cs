using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Domain.Model;

/// <summary>DCA-ERR-004: an inner-layer failure carrying a container stereotype.</summary>
[DomainCentric.ArchRules.Tests.Fixtures.Errors.Roles.Container.Classified]
public sealed class ReservationOverbookedException : DomainException
{
    public ReservationOverbookedException(ReservationId id)
        : base($"reservation {id.Value} exceeds the capacity")
    {
    }
}
