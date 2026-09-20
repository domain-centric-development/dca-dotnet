using DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Application.Shared;

public interface IReservationRepository : IRepository<Reservation, ReservationId>
{
}
