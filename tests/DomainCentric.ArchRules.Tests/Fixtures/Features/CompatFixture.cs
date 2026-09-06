// Compatibility fixture — a bounded context whose use cases are grouped by feature
// (Application.Ordering.PlaceOrder) with the incoming web adapter mirroring the feature
// (Adapter.Incoming.Web.Ordering). The whole catalog must pass against it: every rule selects with
// "below the layer namespace", so a nested feature namespace stays governed. If a selector ever
// regresses to a direct-child assumption, this fixture fails first.
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Compat.Sales
{
    [BoundedContext("Sales", Description = "Fixture context whose use cases are grouped by feature")]
    public static class SalesContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Compat.SharedKernel
{
    [global::DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships.SharedKernel(Description = "shared")]
    public static class SharedKernelContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Compat.Sales.Domain.Model
{
    public readonly record struct OrderId(Guid Value) : IId;

    public sealed record OrderPlaced(OrderId OrderId) : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();

        public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    }

    public sealed class Order : AggregateRootBase<Order, OrderId>
    {
        private Order(OrderId id)
        {
            Id = id;
        }

        public override OrderId Id { get; }

        public static Order Place(OrderId id)
        {
            var order = new Order(id);
            order.RegisterEvent(new OrderPlaced(id));
            return order;
        }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Compat.Sales.Application.Shared
{
    using Domain.Model;

    public interface IOrderRepository : IRepository<Order, OrderId>
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Compat.Sales.Application.Ordering.PlaceOrder
{
    using Domain.Model;
    using Shared;

    public interface IPlaceOrderInputPort : IUseCase<PlaceOrderCommand, PlaceOrderResult>
    {
    }

    public sealed record PlaceOrderCommand(Guid OrderId);

    public sealed record PlaceOrderResult(Guid OrderId, bool Cleared);

    /// <summary>Use-case-local output port: only placing an order asks the fraud check.</summary>
    public interface IFraudCheckPort : IOutputPort
    {
        Task<bool> ClearsAsync(OrderId orderId, CancellationToken cancellationToken = default);
    }

    public sealed class PlaceOrderUseCase : IPlaceOrderInputPort
    {
        private readonly IOrderRepository _orders;
        private readonly IFraudCheckPort _fraudCheck;
        private readonly IDomainEventPublisher _events;

        public PlaceOrderUseCase(IOrderRepository orders, IFraudCheckPort fraudCheck, IDomainEventPublisher events)
        {
            _orders = orders;
            _fraudCheck = fraudCheck;
            _events = events;
        }

        public async Task<PlaceOrderResult> ExecuteAsync(PlaceOrderCommand command, CancellationToken cancellationToken = default)
        {
            var id = new OrderId(command.OrderId);
            var cleared = await _fraudCheck.ClearsAsync(id, cancellationToken);
            var order = Order.Place(id);
            await _orders.SaveAsync(order, cancellationToken);
            await _events.PublishAndClearEventsAsync(order, cancellationToken);
            return new PlaceOrderResult(order.Id.Value, cleared);
        }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Compat.Sales.Adapter.Incoming.Web.Ordering
{
    using Application.Ordering.PlaceOrder;

    public sealed class OrderPageController : PageModel
    {
        private readonly IPlaceOrderInputPort _placeOrder;

        public OrderPageController(IPlaceOrderInputPort placeOrder)
        {
            _placeOrder = placeOrder;
        }

        public async Task<PlaceOrderPageViewModel> PlaceAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            var result = await _placeOrder.ExecuteAsync(new PlaceOrderCommand(orderId), cancellationToken);
            return new PlaceOrderPageViewModel(result.OrderId);
        }
    }

    public sealed record PlaceOrderPageViewModel(Guid OrderId);
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Compat.Sales.Adapter.Outgoing.Persistence
{
    using Application.Shared;
    using Domain.Model;

    public sealed class InMemoryOrderRepository : IOrderRepository
    {
        private readonly ConcurrentDictionary<OrderId, Order> _store = new();

        public Task<Order?> FindByIdAsync(OrderId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.TryGetValue(id, out var order) ? order : null);

        public Task<Order> SaveAsync(Order aggregate, CancellationToken cancellationToken = default)
        {
            _store[aggregate.Id] = aggregate;
            return Task.FromResult(aggregate);
        }

        public Task DeleteByIdAsync(OrderId id, CancellationToken cancellationToken = default)
        {
            _store.TryRemove(id, out _);
            return Task.CompletedTask;
        }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Compat.Sales.Adapter.Outgoing.Fraud
{
    using Application.Ordering.PlaceOrder;
    using Domain.Model;

    public sealed class AlwaysClearsFraudCheck : IFraudCheckPort
    {
        public Task<bool> ClearsAsync(OrderId orderId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
