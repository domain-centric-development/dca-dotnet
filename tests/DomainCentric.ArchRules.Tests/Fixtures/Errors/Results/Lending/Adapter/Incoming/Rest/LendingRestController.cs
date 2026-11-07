using System;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Errors.Results.Lending.Application.ReturnCopy;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Results.Lending.Adapter.Incoming.Rest;

/// <summary>Drives a use case with neither a failure handler nor a switch over a closed result.</summary>
public sealed class LendingRestController
{
    private readonly IReturnCopyInputPort _returnCopy;

    public LendingRestController(IReturnCopyInputPort returnCopy)
    {
        _returnCopy = returnCopy;
    }

    public async Task<string> ReturnAsync(string loanId)
    {
        try
        {
            return (await _returnCopy.ExecuteAsync(new ReturnCopyCommand(loanId))).LoanId;
        }
        catch (InvalidOperationException)
        {
            return "500";
        }
    }
}
