// RetrofitOpen fixture — what the retrofit configuration must still report: an outcome type open to any
// subclass, and a store port in the application layer that does not extend the store role.
namespace DomainCentric.ArchRules.Tests.Fixtures.RetrofitOpen.Billing.Domain.Model
{
    /// <summary>DCA-USE-006: abstract, but its constructor is protected — any type may derive from it.</summary>
    public abstract class RefundResult
    {
        protected RefundResult()
        {
        }
    }

    public sealed class Refunded : RefundResult
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.RetrofitOpen.Billing.Application.Shared
{
    /// <summary>DCA-TAC-018: a store port in the application layer that does not extend IStore.</summary>
    public interface IReceiptStore
    {
        void Keep(string receipt);
    }
}
