using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Application;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using OneOf;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Results.Lending.Application.RenewLoan;

public sealed record RenewLoanCommand(string LoanId);

/// <summary>A closed result returned by a port method of its own name.</summary>
public sealed class RenewLoanResult : OneOfBase<RenewLoanResult.Renewed, RenewLoanResult.Reserved>
{
    public RenewLoanResult(Renewed renewed)
        : base(renewed)
    {
    }

    public RenewLoanResult(Reserved reserved)
        : base(reserved)
    {
    }

    public sealed record Renewed;

    public sealed record Reserved : IRefusal;
}

public interface IRenewLoanInputPort : IInputPort
{
    Task<RenewLoanResult> RenewAsync(RenewLoanCommand command);
}
