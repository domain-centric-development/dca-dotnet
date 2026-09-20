using DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Application.Shared;

public interface IReservationRepository : IRepository<Reservation, ReservationId>
{
}
