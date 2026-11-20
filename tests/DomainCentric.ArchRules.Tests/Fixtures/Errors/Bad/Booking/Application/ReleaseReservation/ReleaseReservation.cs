using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Application.ReleaseReservation;

public interface IReleaseReservationInputPort : IUseCase<ReleaseReservationCommand, ReleaseReservationResult>
{
}

public sealed record ReleaseReservationCommand(Guid ReservationId);

public sealed record ReleaseReservationResult(bool Released);

/// <summary>DCA-ERR-010: catches every failure around its output port.</summary>
public sealed class ReleaseReservationUseCase : IReleaseReservationInputPort
{
    private readonly IReservationRepository _reservations;

    public ReleaseReservationUseCase(IReservationRepository reservations)
    {
        _reservations = reservations;
    }

    public async Task<ReleaseReservationResult> ExecuteAsync(
        ReleaseReservationCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var reservation = await _reservations.FindByIdAsync(new ReservationId(command.ReservationId), cancellationToken);
            return new ReleaseReservationResult(reservation is not null);
        }
        catch (Exception)
        {
            return new ReleaseReservationResult(false);
        }
    }
}
