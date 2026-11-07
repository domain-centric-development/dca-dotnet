using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Results.Lending.Domain.Model;

public sealed class LoanLimitReachedException : DomainException
{
    public LoanLimitReachedException(int limit)
        : base($"loan limit of {limit} reached")
    {
    }
}
