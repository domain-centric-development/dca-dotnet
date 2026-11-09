using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Adapter.Outgoing;

/// <summary>DCA-HEX-021: an outgoing adapter that changes the aggregate it should only store.</summary>
public sealed class OrderArchive
{
    public async Task ArchiveAsync(Order order)
    {
        await Task.Yield();
        order.Cancel();
    }

    public void Annotate(Order order) => order.Note = "archived";

    public int Remind(Order order) => order.Remind();

    /// <summary>A query and a read pass.</summary>
    public bool Large(Order order) => order.IsLargerThan(order.Total) && !order.Cancelled;
}
