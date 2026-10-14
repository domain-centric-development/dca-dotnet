using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Ordering.Application.PlaceOrder;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.SharedKernel.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Ordering.Adapter.Incoming;

[ApiController]
public sealed class OrderResource : ControllerBase
{
    private readonly IPlaceOrderInputPort _placeOrder;

    public OrderResource(IPlaceOrderInputPort placeOrder)
    {
        _placeOrder = placeOrder;
    }

    // DCA-HEX-014: the command is reached by POST; DCA-HEX-015: the notice travels as a code in the URL
    [HttpPost]
    public Task<PlaceOrderResult> Place(decimal amount, CancellationToken cancellationToken) =>
        _placeOrder.ExecuteAsync(new PlaceOrderCommand(new Money(amount, "EUR")), cancellationToken);

    [HttpGet]
    public string Confirmation(string notice) => notice == "placed" ? "placed" : "";
}
