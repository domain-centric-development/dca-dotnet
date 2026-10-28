using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Errors.OwnFailures.Booking.Application.PlaceBooking;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.OwnFailures.Booking.Adapter.Incoming.Rest;

public sealed class BookingRestController
{
    private readonly IPlaceBookingInputPort _placeBooking;

    public BookingRestController(IPlaceBookingInputPort placeBooking)
    {
        _placeBooking = placeBooking;
    }

    public async Task<string> PlaceAsync(string slot)
    {
        try
        {
            return (await _placeBooking.ExecuteAsync(new PlaceBookingCommand(slot))).BookingId;
        }
        catch (SlotTakenException)
        {
            return "409";
        }
    }
}
