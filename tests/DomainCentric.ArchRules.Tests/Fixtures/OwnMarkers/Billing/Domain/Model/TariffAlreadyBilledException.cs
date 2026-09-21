using DomainCentric.ArchRules.Tests.Fixtures.OwnMarkers.Vocabulary;

namespace DomainCentric.ArchRules.Tests.Fixtures.OwnMarkers.Billing.Domain.Model;

/// <summary>A domain failure in its own layer — nothing for DCA-ERR-002 to report.</summary>
public sealed class TariffAlreadyBilledException : ContractFailure
{
    public TariffAlreadyBilledException(string tariff) : base($"tariff {tariff} is already billed")
    {
    }
}
