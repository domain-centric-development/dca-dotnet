using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Domain.Model;

public sealed class ReservationAlreadyConfirmedException : DomainException
{
    public ReservationAlreadyConfirmedException(ReservationId id)
        : base($"reservation {id.Value} is already confirmed")
    {
    }
}
