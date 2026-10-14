using System;
using System.Threading.Tasks;

namespace DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Api;

/// <summary>DCA-NAM-013: a published operation named after the field it writes.</summary>
public interface IOrderLookup
{
    Task UpdateStatusAsync(Guid orderId, string status);
}
