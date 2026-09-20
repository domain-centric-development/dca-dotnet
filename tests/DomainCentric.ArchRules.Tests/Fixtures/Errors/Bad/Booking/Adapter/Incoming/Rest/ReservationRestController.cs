using System;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Application.ConfirmReservation;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Adapter.Incoming.Rest;

/// <summary>DCA-ERR-006: drives an input port and names no failure type of the inner layers.</summary>
public sealed class ReservationRestController
{
    private readonly IConfirmReservationInputPort _confirmReservation;

    public ReservationRestController(IConfirmReservationInputPort confirmReservation)
    {
        _confirmReservation = confirmReservation;
    }

    public async Task<string> ConfirmAsync(Guid reservationId)
    {
        try
        {
            var result = await _confirmReservation.ExecuteAsync(new ConfirmReservationCommand(reservationId));
            return result.ReservationId.ToString();
        }
        catch (Exception)
        {
            return "500";
        }
    }
}
