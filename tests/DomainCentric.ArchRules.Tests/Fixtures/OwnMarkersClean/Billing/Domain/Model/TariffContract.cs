using DomainCentric.ArchRules.Tests.Fixtures.OwnMarkersClean.Vocabulary;

namespace DomainCentric.ArchRules.Tests.Fixtures.OwnMarkersClean.Billing.Domain.Model;

/// <summary>An aggregate that depends on nothing but its own vocabulary and the platform.</summary>
public sealed class TariffContract(string tariff) : IContractRoot
{
    public string Tariff { get; } = tariff;
}
