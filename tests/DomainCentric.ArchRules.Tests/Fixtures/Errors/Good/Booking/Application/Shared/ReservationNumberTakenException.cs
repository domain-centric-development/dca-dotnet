using DomainCentric.BuildingBlocks.Application;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Application.Shared;

/// <summary>Declared by the repository port: a number only the store can see is taken. The adapter raises it.</summary>
public sealed class ReservationNumberTakenException : OutputPortException
{
    public ReservationNumberTakenException(string number)
        : base($"Reservation number {number} is taken")
    {
    }
}
