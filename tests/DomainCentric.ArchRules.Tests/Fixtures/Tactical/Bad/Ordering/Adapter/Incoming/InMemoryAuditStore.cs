using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Application.PlaceOrder;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Adapter.Incoming;

/// <summary>DCA-TAC-020: store implementation outside Adapter.Outgoing.</summary>
public sealed class InMemoryAuditStore : IAuditStore
{
    public Task SaveAsync(string entry) => Task.CompletedTask;
}
