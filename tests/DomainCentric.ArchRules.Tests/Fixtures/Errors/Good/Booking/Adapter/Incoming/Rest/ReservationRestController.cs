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

    /// <summary>DCA-ERR-010 passes: a catch of everything that hands the same failure on.</summary>
    public async Task<string> ConfirmCountedAsync(Guid reservationId)
    {
        try
        {
            var result = await _confirmReservation.ExecuteAsync(new ConfirmReservationCommand(reservationId));
            return result.ReservationId.ToString();
        }
        catch (Exception)
        {
            Failures++;
            throw;
        }
    }

    /// <summary>DCA-ERR-010 passes: an await in the clause, then the same failure handed on.</summary>
    public async Task<string> ConfirmWithCleanUpAsync(Guid reservationId)
    {
        try
        {
            var result = await _confirmReservation.ExecuteAsync(new ConfirmReservationCommand(reservationId));
            return result.ReservationId.ToString();
        }
        catch (Exception)
        {
            await Task.Yield();
            throw;
        }
    }

    /// <summary>DCA-ERR-010 passes: a catch of everything that undoes its own work and hands the same failure on.</summary>
    public async Task<string> ConfirmAndUndoAsync(Guid reservationId)
    {
        Pending++;
        try
        {
            var result = await _confirmReservation.ExecuteAsync(new ConfirmReservationCommand(reservationId));
            return result.ReservationId.ToString();
        }
        catch (Exception)
        {
            Undo();
            throw;
        }
    }

    /// <summary>DCA-ERR-010 passes: the same without an async state machine.</summary>
    public string ConfirmAndUndo(Guid reservationId)
    {
        Pending++;
        try
        {
            return _confirmReservation.ExecuteAsync(new ConfirmReservationCommand(reservationId)).GetAwaiter().GetResult().ReservationId.ToString();
        }
        catch (Exception)
        {
            Undo();
            throw;
        }
    }

    public int Failures { get; private set; }

    public int Pending { get; private set; }

    private void Undo() => Pending--;
}
