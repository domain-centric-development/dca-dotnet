using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Errors.Results.Lending.Domain.Model;
using DomainCentric.BuildingBlocks.Application;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using OneOf;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Results.Lending.Application.LendCopy;

public sealed record LendCopyCommand(string MemberId, string CopyId);

/// <summary>The answer of the use case: lent, or the expected refusal as a case.</summary>
public sealed class LendCopyResult : OneOfBase<LendCopyResult.Lent, LendCopyResult.LimitReached>
{
    public LendCopyResult(Lent lent)
        : base(lent)
    {
    }

    public LendCopyResult(LimitReached refused)
        : base(refused)
    {
    }

    public sealed record Lent(string LoanId);

    public sealed record LimitReached(int Limit) : IRefusal;
}

public interface ILendCopyInputPort : IUseCase<LendCopyCommand, LendCopyResult>
{
}

public sealed class LendCopyUseCase : ILendCopyInputPort
{
    public Task<LendCopyResult> ExecuteAsync(LendCopyCommand input, CancellationToken cancellationToken = default)
    {
        try
        {
            if (input.CopyId.Length == 0)
            {
                throw new LoanLimitReachedException(3);
            }

            return Task.FromResult(new LendCopyResult(new LendCopyResult.Lent(input.MemberId + "-" + input.CopyId)));
        }
        catch (LoanLimitReachedException)
        {
            return Task.FromResult(new LendCopyResult(new LendCopyResult.LimitReached(3)));
        }
    }
}
