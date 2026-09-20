using System;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Domain.Model;

/// <summary>DCA-ERR-001: a failure of the model without the domain base type.</summary>
public sealed class ReservationAlreadyConfirmedException : Exception
{
    public ReservationAlreadyConfirmedException(ReservationId id)
        : base($"reservation {id.Value} is already confirmed")
    {
    }
}
