using System;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Application.ConfirmReservation;

/// <summary>DCA-ERR-003: a use-case failure without the application base type.</summary>
public sealed class ReservationNotFoundException : Exception
{
    public ReservationNotFoundException(Guid reservationId)
        : base($"no reservation {reservationId}")
    {
    }
}
