using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Domain.Model;

public sealed class Reservation : AggregateRootBase<Reservation, ReservationId>
{
    private bool _confirmed;

    public Reservation(ReservationId id)
    {
        Id = id;
    }

    public override ReservationId Id { get; }

    public void Confirm()
    {
        if (_confirmed)
        {
            throw new ReservationAlreadyConfirmedException(Id);
        }

        _confirmed = true;
    }
}
