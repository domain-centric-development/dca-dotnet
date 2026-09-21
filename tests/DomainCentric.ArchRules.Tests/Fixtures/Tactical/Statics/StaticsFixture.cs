// A static repository reference is a port in the aggregate all the same (DCA-TAC-002 reports it); a static
// member holds no instance state, so DCA-TAC-003, DCA-TAC-007 and DCA-TAC-008 all pass over one.
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

    public sealed class InvoiceLine : IEntity<InvoiceLine, InvoiceId>
    {
        // DCA-TAC-007: a static aggregate reference is not this entity's state.
        public static Invoice? LastInvoice;

        internal InvoiceLine(InvoiceId id)
        {
            Id = id;
        }

        public InvoiceId Id { get; }
    }

    public sealed record Discount(decimal Percentage) : IValue
    {
        // DCA-TAC-008: a static entity reference is not this value object's state.
        public static InvoiceLine? LastLine;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Statics.Billing.Application.Shared
{
    using Domain.Model;

    public interface IInvoiceRepository : IRepository<Invoice, InvoiceId>
    {
    }
}
