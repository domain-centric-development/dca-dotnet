using System;
using DomainCentric.BuildingBlocks.Application;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Adapter.Incoming.Rest;

/// <summary>DCA-ERR-002: a use-case failure declared in the adapter layer.</summary>
public sealed class ReservationUnavailableException : UseCaseException
{
    public ReservationUnavailableException(Guid reservationId)
        : base($"reservation {reservationId} cannot be served right now")
    {
    }
}
