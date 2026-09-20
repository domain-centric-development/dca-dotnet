using System;
using DomainCentric.BuildingBlocks.Application;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Application.ConfirmReservation;

public sealed class ReservationNotFoundException : UseCaseException
{
    public ReservationNotFoundException(Guid reservationId)
        : base($"no reservation {reservationId}")
    {
    }
}
