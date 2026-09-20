using System;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Application.ConfirmReservation;
using DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Adapter.Incoming.Rest;

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
        catch (ReservationNotFoundException)
        {
            return "404";
        }
        catch (ReservationAlreadyConfirmedException)
        {
            return "409";
        }
    }
}
