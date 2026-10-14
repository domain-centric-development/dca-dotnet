using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.Billing.IssueInvoice;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.Fulfilment.ShipOrderLater;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Adapter.Incoming.Web;

/// <summary>DCA-USE-018: one adapter that drives the input ports of two features.</summary>
public sealed class OrderDeskPageController : PageModel
{
    private readonly IShipOrderLaterInputPort _shipLater;
    private readonly IIssueInvoiceInputPort _issueInvoice;

    public OrderDeskPageController(IShipOrderLaterInputPort shipLater, IIssueInvoiceInputPort issueInvoice)
    {
        _shipLater = shipLater;
        _issueInvoice = issueInvoice;
    }
}
