// Authorization fixture - DCA-HEX-020 lists a controller that branches on the caller's role in its own code, and not
// one that declares the claims-only check with the authorization attribute.
namespace DomainCentric.ArchRules.Tests.Fixtures.Authorization.Bad.Ledger.Adapter.Incoming.Web
{
    /// <summary>The request's view of the caller, as the protocol offers it.</summary>
    public interface ICaller
    {
        bool IsInRole(string role);
    }

    public sealed class LedgerController : Microsoft.AspNetCore.Mvc.ControllerBase
    {
        public string CloseBooks(ICaller caller)
        {
            if (!caller.IsInRole("Accountant"))
            {
                return "forbidden";
            }

            return "closed";
        }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Authorization.Good.Ledger.Adapter.Incoming.Web
{
    /// <summary>The request's view of the caller, as the protocol offers it.</summary>
    public interface ICaller
    {
        bool IsInRole(string role);
    }

    public sealed class LedgerController : Microsoft.AspNetCore.Mvc.ControllerBase
    {
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Accountant")]
        public string CloseBooks(ICaller caller) => "closed";
    }
}
