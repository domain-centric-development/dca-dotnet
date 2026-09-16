// Context-map fixture — every declaration is Planned, so DCA-MAP-008/009/010 do not enforce it yet
// (like DCA-MAP-007), while DCA-MAP-011 counts it as declared.
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
    using External.Tax;
    using Ledger.Api;
    using Ledger.Events;

    /// <summary>
    /// Would violate DCA-MAP-008 (Api contract outside the outgoing adapter), DCA-MAP-009 (Events contract in the
    /// domain) and DCA-MAP-010 (external contract outside the adapter) - but every declaration is Planned, so none
    /// of the three enforces it yet.
    /// </summary>
    public sealed record Invoice(ILedgerApi Ledger, EntryPosted Posted, TaxClient Tax);
}
