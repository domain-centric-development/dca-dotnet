using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.OwnFailures.Booking.Application.PlaceBooking;

public sealed record PlaceBookingCommand(string Slot);

public sealed record PlaceBookingResult(string BookingId);

public interface IPlaceBookingInputPort : IUseCase<PlaceBookingCommand, PlaceBookingResult>
{
}

/// <summary>A failure type of the project's own, not derived from the building blocks' failure markers.</summary>
public sealed class SlotTakenException : Exception
{
    public SlotTakenException(string slot)
        : base("slot taken: " + slot)
    {
    }
}
