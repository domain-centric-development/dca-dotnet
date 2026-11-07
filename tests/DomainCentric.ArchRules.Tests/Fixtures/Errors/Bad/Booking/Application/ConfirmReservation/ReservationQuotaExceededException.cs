using DomainCentric.BuildingBlocks.Application;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Application.ConfirmReservation;

/// <summary>DCA-ERR-003: derives from the classification instead of UseCaseException.</summary>
public sealed class ReservationQuotaExceededException : InputPortException
{
    public ReservationQuotaExceededException(int quota)
        : base($"Reservation quota of {quota} exceeded")
    {
    }
}
