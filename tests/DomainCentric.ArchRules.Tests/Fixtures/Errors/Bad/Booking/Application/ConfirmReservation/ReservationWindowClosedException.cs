using System;
using DomainCentric.BuildingBlocks.Application;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Application.ConfirmReservation;

/// <summary>DCA-ERR-004: an inner-layer failure that fixes its own protocol answer.</summary>
[DomainCentric.ArchRules.Tests.Fixtures.Errors.Roles.Transport.FixesAnswer]
public sealed class ReservationWindowClosedException : UseCaseException
{
    public ReservationWindowClosedException(Guid reservationId)
        : base($"the confirmation window of reservation {reservationId} has closed")
    {
    }
}
