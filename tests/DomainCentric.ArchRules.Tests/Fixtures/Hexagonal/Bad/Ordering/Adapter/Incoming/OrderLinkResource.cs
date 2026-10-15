using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Application.PlaceOrder;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.SharedKernel.Domain.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Adapter.Incoming;

/// <summary>DCA-HEX-014: a link that places.</summary>
[ApiController]
public sealed class OrderLinkResource : ControllerBase
{
    private readonly IPlaceOrderInputPort _placeOrder;

    public OrderLinkResource(IPlaceOrderInputPort placeOrder)
    {
        _placeOrder = placeOrder;
    }

    [HttpGet]
    public Task<PlaceOrderResult> Reorder(decimal amount, CancellationToken cancellationToken) => Submit(amount, cancellationToken);

    [Route("orders/repeat")]
    [AcceptVerbs("GET", "HEAD")]
    public Task<PlaceOrderResult> Repeat(decimal amount, CancellationToken cancellationToken) => Submit(amount, cancellationToken);

    // A route without an HTTP method answers every method, GET included.
    [Route("orders/again")]
    public Task<PlaceOrderResult> Again(decimal amount, CancellationToken cancellationToken) => Submit(amount, cancellationToken);

    // Conventional routing: a public action without a verb attribute answers every method.
    public Task<PlaceOrderResult> Renew(decimal amount, CancellationToken cancellationToken) => Submit(amount, cancellationToken);

    private Task<PlaceOrderResult> Submit(decimal amount, CancellationToken cancellationToken) =>
        _placeOrder.ExecuteAsync(new PlaceOrderCommand(new Money(amount, "EUR")), cancellationToken);
}

/// <summary>DCA-HEX-014 by the handler-name convention.</summary>
public sealed class ReorderPage : PageModel
{
    private readonly IPlaceOrderInputPort _placeOrder;

    public ReorderPage(IPlaceOrderInputPort placeOrder)
    {
        _placeOrder = placeOrder;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        await _placeOrder.ExecuteAsync(new PlaceOrderCommand(new Money(1m, "EUR")), cancellationToken);

    // A page model routes by handler name: a public method without the prefix is no handler.
    public Task<PlaceOrderResult> Preview(CancellationToken cancellationToken) =>
        _placeOrder.ExecuteAsync(new PlaceOrderCommand(new Money(1m, "EUR")), cancellationToken);
}
