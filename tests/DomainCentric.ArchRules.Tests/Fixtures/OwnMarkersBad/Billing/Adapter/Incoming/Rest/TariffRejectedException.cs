using DomainCentric.ArchRules.Tests.Fixtures.OwnMarkersBad.Vocabulary;

namespace DomainCentric.ArchRules.Tests.Fixtures.OwnMarkersBad.Billing.Adapter.Incoming.Rest;

/// <summary>DCA-ERR-002: a failure of the model declared in an incoming adapter. Reached only when the
/// role's type is matched through a base type.</summary>
public sealed class TariffRejectedException : ContractFailure
{
    public TariffRejectedException(string tariff) : base($"tariff {tariff} was rejected")
    {
    }
}
