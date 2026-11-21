using System;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Application.ConfirmReservation;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Adapter.Incoming.Rest;

/// <summary>DCA-ERR-010: catches of every failure that do not hand the caught failure on unchanged.</summary>
public sealed class ReservationRetryController
{
    private readonly IConfirmReservationInputPort _confirmReservation;

    public ReservationRetryController(IConfirmReservationInputPort confirmReservation)
    {
        _confirmReservation = confirmReservation;
    }

    public int Failures { get; private set; }

    /// <summary>Rethrows on one path and answers on the other.</summary>
    public async Task<string> RethrowOrAnswerAsync(Guid reservationId)
    {
        try
        {
            var result = await _confirmReservation.ExecuteAsync(new ConfirmReservationCommand(reservationId));
            return result.ReservationId.ToString();
        }
        catch (Exception)
        {
            if (++Failures > 2)
            {
                throw;
            }

            return "503";
        }
    }

    /// <summary>Wraps the caught failure into another one.</summary>
    public async Task<string> WrapAsync(Guid reservationId)
    {
        try
        {
            var result = await _confirmReservation.ExecuteAsync(new ConfirmReservationCommand(reservationId));
            return result.ReservationId.ToString();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("confirmation failed", e);
        }
    }

    /// <summary>Throws the caught failure again as a new throw, which starts a new stack trace.</summary>
    public async Task<string> ThrowCaughtAsync(Guid reservationId)
    {
        try
        {
            var result = await _confirmReservation.ExecuteAsync(new ConfirmReservationCommand(reservationId));
            return result.ReservationId.ToString();
        }
        catch (Exception e)
        {
            Failures++;
#pragma warning disable CA2200 // the fixture shows the throw that resets the stack trace
            throw e;
#pragma warning restore CA2200
        }
    }

    /// <summary>Throws something else.</summary>
    public async Task<string> ThrowOtherAsync(Guid reservationId)
    {
        try
        {
            var result = await _confirmReservation.ExecuteAsync(new ConfirmReservationCommand(reservationId));
            return result.ReservationId.ToString();
        }
        catch (Exception)
        {
            throw new TimeoutException();
        }
    }

    /// <summary>Cleans up with an await, then answers.</summary>
    public async Task<string> AwaitThenAnswerAsync(Guid reservationId)
    {
        try
        {
            var result = await _confirmReservation.ExecuteAsync(new ConfirmReservationCommand(reservationId));
            return result.ReservationId.ToString();
        }
        catch (Exception)
        {
            await Task.Yield();
            return "500";
        }
    }

    /// <summary>Cleans up with an await, then throws the caught failure again as a new throw.</summary>
    public async Task<string> AwaitThenThrowCaughtAsync(Guid reservationId)
    {
        try
        {
            var result = await _confirmReservation.ExecuteAsync(new ConfirmReservationCommand(reservationId));
            return result.ReservationId.ToString();
        }
        catch (Exception e)
        {
            await Task.Yield();
#pragma warning disable CA2200 // the fixture shows the throw that resets the stack trace
            throw e;
#pragma warning restore CA2200
        }
    }

    /// <summary>Swallows the failure without an answer.</summary>
    public async Task SwallowAsync(Guid reservationId)
    {
        try
        {
            await _confirmReservation.ExecuteAsync(new ConfirmReservationCommand(reservationId));
        }
        catch (Exception)
        {
            Failures++;
        }
    }
}
