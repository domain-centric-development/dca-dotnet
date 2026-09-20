using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Adapter.Outgoing;

public sealed class InMemoryReservationRepository : IReservationRepository
{
    private readonly ConcurrentDictionary<ReservationId, Reservation> _store = new();

    public Task<Reservation?> FindByIdAsync(ReservationId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id, out var reservation) ? reservation : null);

    public Task<Reservation> SaveAsync(Reservation aggregate, CancellationToken cancellationToken = default)
    {
        _store[aggregate.Id] = aggregate;
        return Task.FromResult(aggregate);
    }

    public Task DeleteByIdAsync(ReservationId id, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
