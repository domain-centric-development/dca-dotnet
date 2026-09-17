// A static repository reference is a port in the aggregate all the same (DCA-TAC-002 reports it); a static
// template of the own aggregate type carries no aggregate state (DCA-TAC-003 does not).
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Statics.Billing
{
    [BoundedContext("Billing")]
    public static class BillingContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Statics.Billing.Domain.Model
{
    using Application.Shared;

    public readonly record struct InvoiceId(string Value) : IId, IValue;

    public sealed class Invoice : AggregateRootBase<Invoice, InvoiceId>
    {
        internal static IInvoiceRepository? Lookup;
        public static Invoice? Template { get; set; }

        public Invoice(InvoiceId id)
        {
            Id = id;
        }

        public override InvoiceId Id { get; }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Statics.Billing.Application.Shared
{
    using Domain.Model;

    public interface IInvoiceRepository : IRepository<Invoice, InvoiceId>
    {
    }
}
