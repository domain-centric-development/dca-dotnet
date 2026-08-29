using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Application.PlaceOrder;

// DCA-NAM-001: implements an input port but does not end with 'UseCase'
public sealed class PlaceOrderService : IPlaceOrderInputPort
{
    public Task<PlaceOrderResult> ExecuteAsync(PlaceOrderCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PlaceOrderResult(command.OrderId));
}

// DCA-NAM-007: a DTO in the application layer
public sealed record PlaceOrderDto(OrderId OrderId);

// DCA-NAM-011: a ViewModel in the application layer
public sealed record PlaceOrderViewModel(string OrderId);
