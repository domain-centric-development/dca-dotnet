using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Application.PlaceOrder;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.SharedKernel.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Adapter.Incoming;

/// <summary>DCA-HEX-014: a link that places; DCA-HEX-015: state for the next request kept in this instance.</summary>
[ApiController]
public sealed class OrderLinkResource : ControllerBase
{
    private readonly IPlaceOrderInputPort _placeOrder;
    private readonly ISession _session;

    public OrderLinkResource(IPlaceOrderInputPort placeOrder, ISession session)
    {
        _placeOrder = placeOrder;
        _session = session;
    }

    [HttpGet]
    public Task<PlaceOrderResult> Reorder(decimal amount, CancellationToken cancellationToken) => Submit(amount, cancellationToken);

    private Task<PlaceOrderResult> Submit(decimal amount, CancellationToken cancellationToken)
    {
        _session.SetString("notice", "Your order was placed.");
        return _placeOrder.ExecuteAsync(new PlaceOrderCommand(new Money(amount, "EUR")), cancellationToken);
    }
}

/// <summary>DCA-HEX-014 by the handler-name convention; DCA-HEX-015 through a message kept for the next page.</summary>
public sealed class ReorderPage : PageModel
{
    private readonly IPlaceOrderInputPort _placeOrder;

    public ReorderPage(IPlaceOrderInputPort placeOrder)
    {
        _placeOrder = placeOrder;
    }

    [TempData]
    public string? Notice { get; set; }

    public async Task OnGetAsync(ITempDataDictionary tempData, CancellationToken cancellationToken)
    {
        await _placeOrder.ExecuteAsync(new PlaceOrderCommand(new Money(1m, "EUR")), cancellationToken);
        tempData["notice"] = "Your order was placed.";
    }
}
