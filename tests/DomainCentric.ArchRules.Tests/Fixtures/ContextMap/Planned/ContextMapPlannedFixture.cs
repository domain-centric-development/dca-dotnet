// Context-map fixture — every declaration is Planned and no code depends on the upstreams yet: nothing to
// place, nothing to demand (DCA-MAP-007..010 pass), while DCA-MAP-011 counts the declarations as declared.
using System;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Planned.Ledger
{
    [BoundedContext("Ledger")]
    public static class LedgerContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Planned.Ledger.Api
{
    public interface ILedgerApi
    {
        string Balance(string account);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Planned.Ledger.Events
{
    public sealed record EntryPosted(Guid EventId, DateTimeOffset OccurredOn, string Account) : IIntegrationEvent;
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Planned.External.Tax
{
    public sealed class TaxClient
    {
        public string Rate(string region) => region;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Planned.Billing
{
    [BoundedContext("Billing")]
    [Upstream("Ledger", Translation.AntiCorruptionLayer, Consumes.Api, Status = UpstreamStatus.Planned)]
    [Upstream("Ledger", Translation.Conformist, Consumes.Events, Status = UpstreamStatus.Planned)]
    [ExternalUpstream("Tax Authority", Translation.AntiCorruptionLayer, Interaction.Outbound, Status = UpstreamStatus.Planned,
        ContractNamespaces = new[] { "DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Planned.External.Tax" })]
    public static class BillingContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.ContextMap.Planned.Billing.Domain.Model
{
    /// <summary>No code depends on the Planned upstreams yet - nothing to place, nothing to demand.</summary>
    public sealed record Invoice(string Number);
}
