using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Errors.Results.Lending.Application.LendCopy;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Results.Lending.Adapter.Incoming.Feed;

/// <summary>Receives a closed result and looks at none of its cases - a refusal is dropped.</summary>
public sealed class LendingFeedConsumer
{
    private readonly ILendCopyInputPort _lendCopy;

    public LendingFeedConsumer(ILendCopyInputPort lendCopy)
    {
        _lendCopy = lendCopy;
    }

    public async Task<string> OnAsync(string memberId, string copyId)
    {
        LendCopyResult ignored = await _lendCopy.ExecuteAsync(new LendCopyCommand(memberId, copyId));
        return ignored.ToString() ?? "";
    }
}
