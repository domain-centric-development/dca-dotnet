using DomainCentric.ArchRules.Tests.Fixtures.OwnMarkersClean.Vocabulary;

namespace DomainCentric.ArchRules.Tests.Fixtures.OwnMarkersClean.Billing.Domain.Model;

/// <summary>A domain failure in its own layer, carrying the code base's own base type.</summary>
public sealed class TariffAlreadyBilledException(string tariff)
    : ContractFailure($"tariff {tariff} is already billed");
