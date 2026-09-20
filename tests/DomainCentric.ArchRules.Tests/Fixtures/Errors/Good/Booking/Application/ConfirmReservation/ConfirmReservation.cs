using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Application.ConfirmReservation;

public interface IConfirmReservationInputPort : IUseCase<ConfirmReservationCommand, ConfirmReservationResult>
{
}

public sealed record ConfirmReservationCommand(Guid ReservationId);

public sealed record ConfirmReservationResult(Guid ReservationId);

public sealed class ConfirmReservationUseCase : IConfirmReservationInputPort
{
    private readonly IReservationRepository _reservations;

    public ConfirmReservationUseCase(IReservationRepository reservations)
    {
        _reservations = reservations;
    }

    public async Task<ConfirmReservationResult> ExecuteAsync(
        ConfirmReservationCommand command,
        CancellationToken cancellationToken = default)
    {
        var id = new ReservationId(command.ReservationId);
        var reservation = await _reservations.FindByIdAsync(id, cancellationToken)
            ?? throw new ReservationNotFoundException(command.ReservationId);
        reservation.Confirm();
        await _reservations.SaveAsync(reservation, cancellationToken);
        return new ConfirmReservationResult(id.Value);
    }
}
