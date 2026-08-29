using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good.SharedKernel
{
    [DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships.SharedKernel(Description = "shared")]
    public static class SharedKernelContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good.SharedKernel.Domain.Model
{
    public sealed record Money(decimal Amount) : IValue;
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good.Order
{
    [BoundedContext("Order", Description = "orders")]
    public static class OrderContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good.Order.Domain.Model
{
    using DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good.SharedKernel.Domain.Model;

    public readonly record struct OrderId(Guid Value) : IId;

    public sealed class Order : AggregateRootBase<Order, OrderId>
    {
        public Order(OrderId id, Money total)
        {
            Id = id;
            Total = total;
            RegisterEvent(new OrderPlaced(Guid.NewGuid(), DateTimeOffset.UtcNow, id));
        }

        public override OrderId Id { get; }

        public Money Total { get; }
    }

    public sealed record OrderPlaced(Guid EventId, DateTimeOffset OccurredOn, OrderId OrderId) : IDomainEvent;

    public sealed class OrderFactory : IFactory
    {
        public Order Create(Money total) => new Order(new OrderId(Guid.NewGuid()), total);
    }

    public sealed class HighValueOrderSpecification : ISpecification<Order>
    {
        private readonly decimal _threshold;

        public HighValueOrderSpecification(decimal threshold)
        {
            _threshold = threshold;
        }

        public bool IsSatisfiedBy(Order candidate) => candidate.Total.Amount >= _threshold;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good.Order.Domain.Service
{
    using DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good.SharedKernel.Domain.Model;

    public sealed class OrderTotalCalculator : IDomainService
    {
        private readonly decimal _taxRate;

        public OrderTotalCalculator(decimal taxRate)
        {
            _taxRate = taxRate;
        }

        public Money WithTax(Money net) => new Money(net.Amount * (1 + _taxRate));
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good.Order.Events
{
    [IntegrationEventType("order.placed", Version = 1)]
    public sealed record OrderPlacedEvent(Guid EventId, DateTimeOffset OccurredOn, string OrderId) : IIntegrationEvent;
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good.Order.Application.Shared
{
    using DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good.Order.Domain.Model;

    public interface IOrderRepository : IRepository<Order, OrderId>
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good.Order.Application.PlaceOrder
{
    using DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good.Order.Application.Shared;
    using DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good.Order.Domain.Model;
    using DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good.SharedKernel.Domain.Model;

    public sealed record PlaceOrderCommand(decimal Total);

    public sealed record PlaceOrderResult(OrderId OrderId);

    public interface IPlaceOrderInputPort : IUseCase<PlaceOrderCommand, PlaceOrderResult>
    {
    }

    public sealed class PlaceOrderUseCase : IPlaceOrderInputPort
    {
        private readonly IOrderRepository _orders;
        private readonly OrderFactory _factory;

        public PlaceOrderUseCase(IOrderRepository orders, OrderFactory factory)
        {
            _orders = orders;
            _factory = factory;
        }

        public async Task<PlaceOrderResult> ExecuteAsync(PlaceOrderCommand input, CancellationToken cancellationToken = default)
        {
            var order = await _orders.SaveAsync(_factory.Create(new Money(input.Total)), cancellationToken);
            return new PlaceOrderResult(order.Id);
        }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good.Order.Adapter.Incoming
{
    using DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good.Order.Application.PlaceOrder;

    public sealed class OrderResource
    {
        private readonly IPlaceOrderInputPort _placeOrder;

        public OrderResource(IPlaceOrderInputPort placeOrder)
        {
            _placeOrder = placeOrder;
        }

        public Task<PlaceOrderResult> PlaceAsync(decimal total) => _placeOrder.ExecuteAsync(new PlaceOrderCommand(total));
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good.Order.Adapter.Outgoing
{
    using System.Collections.Concurrent;
    using DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good.Order.Application.Shared;
    using DomainCentric.ArchRules.Tests.Fixtures.Advanced.Good.Order.Domain.Model;

    public sealed class InMemoryOrderRepository : IOrderRepository
    {
        private readonly ConcurrentDictionary<OrderId, Order> _store = new ConcurrentDictionary<OrderId, Order>();

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
