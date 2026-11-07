using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Errors.Results.Lending.Application.RenewLoan;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Results.Lending.Adapter.Incoming.Web;

/// <summary>Translates with a switch expression over the held case; binds no case to a variable.</summary>
public sealed class LendingWebController
{
    private readonly IRenewLoanInputPort _renewLoan;

    public LendingWebController(IRenewLoanInputPort renewLoan)
    {
        _renewLoan = renewLoan;
    }

    public async Task<string> RenewAsync(string loanId) =>
        (await _renewLoan.RenewAsync(new RenewLoanCommand(loanId))).Value switch
        {
            RenewLoanResult.Renewed => "renewed",
            RenewLoanResult.Reserved => "reserved",
            _ => "unknown",
        };
}
