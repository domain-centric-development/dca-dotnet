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
}
