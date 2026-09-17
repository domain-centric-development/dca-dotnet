// Context-map fixture — every declaration is Planned, yet the code that exists is misplaced: placement is
// checked whatever the status; only DCA-MAP-008's translation-site demand waits for Implemented.
using System;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.PlannedMisplaced.Ledger
{
    [BoundedContext("Ledger")]
    public static class LedgerContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.PlannedMisplaced.Ledger.Api
{
    public interface ILedgerApi
    {
        string Balance(string account);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.PlannedMisplaced.Ledger.Events
{
    public sealed record EntryPosted(Guid EventId, DateTimeOffset OccurredOn, string Account) : IIntegrationEvent;
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.PlannedMisplaced.External.Tax
{
    public sealed class TaxClient
    {
        public string Rate(string region) => region;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.PlannedMisplaced.Billing
{
    [BoundedContext("Billing")]
    [Upstream("Ledger", Translation.AntiCorruptionLayer, Consumes.Api, Status = UpstreamStatus.Planned)]
    [Upstream("Ledger", Translation.Conformist, Consumes.Events, Status = UpstreamStatus.Planned)]
    [ExternalUpstream("Tax Authority", Translation.AntiCorruptionLayer, Interaction.Outbound, Status = UpstreamStatus.Planned,
        ContractNamespaces = new[] { "DomainCentric.ArchRules.Tests.Fixtures.ContextMap.PlannedMisplaced.External.Tax" })]
    public static class BillingContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.PlannedMisplaced.Billing.Domain.Model
{
    using External.Tax;
    using Ledger.Api;
    using Ledger.Events;

    /// <summary>
    /// Misplaced although Planned: the Api contract outside the outgoing adapter (DCA-MAP-008), the Events contract
    /// in the domain (DCA-MAP-009), the external contract outside the adapter (DCA-MAP-010).
    /// </summary>
    public sealed record Invoice(ILedgerApi Ledger, EntryPosted Posted, TaxClient Tax);
}
