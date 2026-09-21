using DomainCentric.ArchRules.Tests.Fixtures.OwnMarkers.Billing.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.OwnMarkers.Vocabulary;

namespace DomainCentric.ArchRules.Tests.Fixtures.OwnMarkers.Billing.Domain.Model;

/// <summary>DCA-TAC-002: an aggregate holding its own repository — visible only once the roles point here.</summary>
public sealed class TariffContract : IContractRoot
{
    private readonly ITariffContractStore _contracts;

    public TariffContract(ITariffContractStore contracts) => _contracts = contracts;

    public bool KnowsItsStore => _contracts is not null;
}
