using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Domain.Model;

/// <summary>DCA-ERR-007: the failure carries the id and the figures the rule compared.</summary>
public sealed class SeatsUnavailableException : DomainException
{
    public SeatsUnavailableException(ReservationId reservationId, int requested, int available)
        : base($"reservation {reservationId.Value}: requested {requested}, available {available}")
    {
        ReservationId = reservationId;
        Requested = requested;
        Available = available;
    }

    public ReservationId ReservationId { get; }

    public int Requested { get; }

    public int Available { get; }
}
