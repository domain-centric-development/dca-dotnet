using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Application.PlaceOrder;

/// <summary>DCA-TAC-019 (not in Application.Shared), DCA-TAC-021 (repository semantics).</summary>
public interface IAuditStore : IStore
{
    Task SaveAsync(string entry);
}
