using DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Domain.Service;

/// <summary>A domain service raises a domain failure: the domain layer refuses, DCA-ERR-009 passes.</summary>
public sealed class ReservationTransfer : IDomainService
{
    public void Transfer(Reservation from, Reservation to)
    {
        if (from.Id.Equals(to.Id))
        {
            throw new ReservationStatusInvalidException(from.Id);
        }
    }
}
