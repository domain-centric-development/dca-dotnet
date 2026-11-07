using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Errors.Results.Lending.Application.LendCopy;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Results.Lending.Adapter.Incoming.Page;

/// <summary>Translates with one function per case of the closed result; touches no member of a case.</summary>
public sealed class LendingPageController
{
    private readonly ILendCopyInputPort _lendCopy;

    public LendingPageController(ILendCopyInputPort lendCopy)
    {
        _lendCopy = lendCopy;
    }

    public async Task<string> LendAsync(string memberId, string copyId)
    {
        var result = await _lendCopy.ExecuteAsync(new LendCopyCommand(memberId, copyId));
        return result.Match(
            lent => "redirect:/loans",
            refused => "loan-limit");
    }
}
