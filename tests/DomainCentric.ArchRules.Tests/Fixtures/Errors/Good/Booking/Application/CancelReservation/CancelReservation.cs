using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Application;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using OneOf;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Application.CancelReservation;

public sealed record CancelReservationCommand(string ReservationId);

/// <summary>A closed result: one success case, the expected refusal marked as such.</summary>
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

    public sealed record NotFound(string ReservationId) : IRefusal;
}

public interface ICancelReservationInputPort : IInputPort
{
    Task<CancelReservationResult> CancelAsync(CancelReservationCommand command);
}
