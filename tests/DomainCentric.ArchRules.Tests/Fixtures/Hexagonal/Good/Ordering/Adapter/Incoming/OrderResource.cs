using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Ordering.Application.PlaceOrder;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.SharedKernel.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Ordering.Adapter.Incoming;

[ApiController]
public sealed class OrderResource : ControllerBase
{
    private readonly IPlaceOrderInputPort _placeOrder;

    public OrderResource(IPlaceOrderInputPort placeOrder)
    {
        _placeOrder = placeOrder;
    }

    // DCA-HEX-014: the command is reached by POST; the GET handler drives none
    [HttpPost]
    public Task<PlaceOrderResult> Place(decimal amount, CancellationToken cancellationToken) =>
        _placeOrder.ExecuteAsync(new PlaceOrderCommand(new Money(amount, "EUR")), cancellationToken);

    // The general verb attribute names POST: not a GET handler.
    [AcceptVerbs("POST")]
    public Task<PlaceOrderResult> PlaceAgain(decimal amount, CancellationToken cancellationToken) =>
        _placeOrder.ExecuteAsync(new PlaceOrderCommand(new Money(amount, "EUR")), cancellationToken);

    // A route without an HTTP method answers every method and is not selected (documented in the rule).
    [Route("orders/any")]
    public Task<PlaceOrderResult> PlaceAnyhow(decimal amount, CancellationToken cancellationToken) =>
        _placeOrder.ExecuteAsync(new PlaceOrderCommand(new Money(amount, "EUR")), cancellationToken);

    [HttpGet]
    public string Confirmation(string notice) => notice == "placed" ? "placed" : "";
}
