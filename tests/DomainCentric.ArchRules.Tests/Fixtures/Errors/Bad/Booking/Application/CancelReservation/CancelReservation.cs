using System;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using OneOf;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Application.CancelReservation;

public sealed record CancelReservationCommand(string ReservationId);

/// <summary>A closed result whose refusing case is not marked: neither its caller nor the boundary can tell.</summary>
public sealed class CancelReservationResult : OneOfBase<CancelReservationResult.Cancelled, CancelReservationResult.NotFound>
{
    public CancelReservationResult(Cancelled cancelled)
        : base(cancelled)
    {
    }

    public CancelReservationResult(NotFound notFound)
        : base(notFound)
    {
    }

    public sealed record Cancelled(string ReservationId);

    public sealed record NotFound(string ReservationId);
}

public interface ICancelReservationInputPort : IInputPort
{
    Task<CancelReservationResult> CancelAsync(CancelReservationCommand command);
}

/// <summary>
/// DCA-ERR-009: invents a domain failure for what the repository did not find - in an async method, so the creation
/// sits in the compiler's state machine, and once more in a lambda, so it sits in a closure.
/// </summary>
public sealed class CancelReservationUseCase : ICancelReservationInputPort
{
    private readonly IReservationRepository _reservations;

    public CancelReservationUseCase(IReservationRepository reservations)
    {
        _reservations = reservations;
    }

    public async Task<CancelReservationResult> CancelAsync(CancelReservationCommand command)
    {
        var id = new ReservationId(Guid.Parse(command.ReservationId));
        _ = await _reservations.FindByIdAsync(id) ?? throw new ReservationUnknownException(id);
        return new CancelReservationResult(new CancelReservationResult.Cancelled(command.ReservationId));
    }

    public Func<Exception> UnknownFor(ReservationId id) => () => new ReservationUnknownException(id);
}
