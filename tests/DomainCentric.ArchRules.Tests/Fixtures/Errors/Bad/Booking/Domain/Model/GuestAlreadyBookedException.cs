using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Domain.Model;

/// <summary>DCA-ERR-007: the failure carries display text and the whole aggregate instead of the id.</summary>
public sealed class GuestAlreadyBookedException : DomainException
{
    private readonly string _guestName;

    public GuestAlreadyBookedException(string guestName, Reservation reservation)
        : base("guest already booked")
    {
        _guestName = guestName;
        Reservation = reservation;
    }

    public Reservation Reservation { get; }
}
