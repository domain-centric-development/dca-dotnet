// Fixture for DotnetRules — every DCA-NET rule passes here.
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Good.SharedKernel
{
    [SharedKernel(Description = "shared")]
    public static class SharedKernelContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Good.SharedKernel.Domain.Model
{
    public sealed record Money(decimal Amount, string Currency) : IValue;
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Good.Order
{
    [BoundedContext("Order", Description = "orders")]
    public static class OrderContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Good.Order.Domain.Model
{
    using DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Good.SharedKernel.Domain.Model;

    public readonly record struct OrderId(Guid Value) : IId;

    public readonly record struct Quantity(int Value) : IValue;

    public sealed record OrderPlaced(Guid EventId, DateTimeOffset OccurredOn, OrderId OrderId) : IDomainEvent;

    public sealed class Order : AggregateRootBase<Order, OrderId>
    {
        public Order(OrderId id, Money total)
        {
            Id = id;
            Total = total;
        }

        public override OrderId Id { get; }

        public Money Total { get; }

        public void Place() => RegisterEvent(new OrderPlaced(Guid.NewGuid(), DateTimeOffset.UtcNow, Id));
    }

    public interface IOrderNumbering : IDomainService
    {
        string Next();
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Good.Order.Application.Shared
{
    using DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Good.Order.Domain.Model;

    public interface IOrderRepository : IRepository<Order, OrderId>
    {
        Task<int> CountAsync(CancellationToken cancellationToken = default);
    }

    public interface IClock : IOutputPort
    {
        DateTimeOffset Now();
    }

    /// <summary>Remote-capable port — called outside the unit of work.</summary>
    public interface ICarrierPort : IOutputPort
    {
        Task<string> QuoteAsync(OrderId orderId, CancellationToken cancellationToken = default);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Good.Order.Application.ShipOrder
{
    using DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Good.Order.Application.Shared;
    using DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Good.Order.Domain.Model;

    public sealed record ShipOrderCommand(OrderId OrderId);

    public sealed record ShipOrderResult(OrderId OrderId, string CarrierQuote);

    public interface IShipOrderInputPort : IUseCase<ShipOrderCommand, ShipOrderResult>
    {
    }

    // DCA-NET-006: transaction boundary through the IUnitOfWork port, remote call outside of it.
    public sealed class ShipOrderUseCase : IShipOrderInputPort
    {
        private readonly IOrderRepository _orders;
        private readonly ICarrierPort _carrier;
        private readonly IUnitOfWork _unitOfWork;

        public ShipOrderUseCase(IOrderRepository orders, ICarrierPort carrier, IUnitOfWork unitOfWork)
        {
            _orders = orders;
            _carrier = carrier;
            _unitOfWork = unitOfWork;
        }

        public async Task<ShipOrderResult> ExecuteAsync(ShipOrderCommand input, CancellationToken cancellationToken = default)
        {
            var quote = await _carrier.QuoteAsync(input.OrderId, cancellationToken).ConfigureAwait(false);
            return await _unitOfWork.RunAsync(
                async ct =>
                {
                    var order = await _orders.FindByIdAsync(input.OrderId, ct).ConfigureAwait(false)
                                ?? throw new InvalidOperationException("unknown order");
                    await _orders.SaveAsync(order, ct).ConfigureAwait(false);
                    return new ShipOrderResult(order.Id, quote);
                },
                cancellationToken).ConfigureAwait(false);
        }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Good.Order.Application.PlaceOrder
{
    using DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Good.Order.Application.Shared;
    using DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Good.Order.Domain.Model;

    public sealed record PlaceOrderCommand(OrderId OrderId);

    public sealed record PlaceOrderResult(OrderId OrderId);

    public interface IPlaceOrderInputPort : IUseCase<PlaceOrderCommand, PlaceOrderResult>
    {
    }

    public sealed class PlaceOrderUseCase : IPlaceOrderInputPort
    {
        private readonly IOrderRepository _orders;

        public PlaceOrderUseCase(IOrderRepository orders)
        {
            _orders = orders;
        }

        public async Task<PlaceOrderResult> ExecuteAsync(PlaceOrderCommand input, CancellationToken cancellationToken = default)
        {
            var order = await _orders.FindByIdAsync(input.OrderId, cancellationToken).ConfigureAwait(false)
                        ?? throw new InvalidOperationException("unknown order");
            order.Place();
            await _orders.SaveAsync(order, cancellationToken).ConfigureAwait(false);
            return new PlaceOrderResult(order.Id);
        }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Good.Order.Adapter.Outgoing
{
    using DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Good.Order.Application.Shared;
    using DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Good.Order.Domain.Model;

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

        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_store.Count);
    }
}
