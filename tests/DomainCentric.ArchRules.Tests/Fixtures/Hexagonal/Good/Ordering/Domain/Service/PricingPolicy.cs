using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.SharedKernel.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Ordering.Domain.Service;

public sealed class PricingPolicy : IDomainService
{
    public Money Discounted(Money total) => total with { Amount = total.Amount * 0.9m };

    public static Money Rounded(Money total) => total with { Amount = decimal.Round(total.Amount, 2) };
}
