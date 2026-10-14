using System;
using System.Threading.Tasks;

namespace DomainCentric.ArchRules.Tests.Fixtures.Naming.Good.Ordering.Api;

/// <summary>DCA-NAM-013: published operations named after what they mean, Settle included.</summary>
public interface IOrderLookup
{
    Task<bool> ExistsAsync(Guid orderId);

    Task SettleAsync(Guid orderId);
}
