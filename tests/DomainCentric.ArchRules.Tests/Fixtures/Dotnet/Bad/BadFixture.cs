// Fixture for DotnetRules — one violation per DCA-NET rule.
using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Bad.SharedKernel
{
    [SharedKernel(Description = "shared")]
    public static class SharedKernelContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Bad.Order
{
    [BoundedContext("Order", Description = "orders")]
    public static class OrderContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Bad.Order.Domain.Model
{
    // DCA-NET-005: identifier is a plain struct, not a record struct.
    public readonly struct OrderId : IId
    {
        public OrderId(Guid value)
        {
            Value = value;
        }

        public Guid Value { get; }
    }

    // DCA-NET-004: value object is a plain class.
    public sealed class Money : IValue
    {
        public Money(decimal amount)
        {
            Amount = amount;
        }

        public decimal Amount { get; }
    }

    public sealed class Order : AggregateRootBase<Order, OrderId>
    {
        public Order(OrderId id)
        {
            Id = id;
        }

        public override OrderId Id { get; }
    }

    // DCA-NET-001: async leaks into the domain.
    public interface IAsyncPricing : IDomainService
    {
        Task<Money> PriceAsync(Order order, CancellationToken cancellationToken);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Bad.Order.Application.ShipOrder
{
    // DCA-NET-006: draws the transaction with System.Transactions inside the use case.
    public sealed class ShipOrderUseCase
    {
        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            using var scope = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeAsyncFlowOption.Enabled);
            scope.Complete();
            return Task.CompletedTask;
        }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Bad.Order.Application.Shared
{
    using DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Bad.Order.Domain.Model;

    // DCA-NET-002: awaitable method without suffix, suffixed method without Task.
    public interface IOrderRepository : IRepository<Order, OrderId>
    {
        Task<int> Count(CancellationToken cancellationToken = default);

        int SizeAsync();
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Bad.Order.Application.PlaceOrder
{
    using DomainCentric.ArchRules.Tests.Fixtures.Dotnet.Bad.Order.Domain.Model;

    public sealed record PlaceOrderCommand(OrderId OrderId);

    public sealed record PlaceOrderResult(OrderId OrderId);

    public interface IPlaceOrderInputPort : IUseCase<PlaceOrderCommand, PlaceOrderResult>
    {
    }

    // DCA-NET-003: two public ExecuteAsync entry points.
    public sealed class PlaceOrderUseCase : IPlaceOrderInputPort
    {
        public Task<PlaceOrderResult> ExecuteAsync(PlaceOrderCommand input, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PlaceOrderResult(input.OrderId));

        public Task<PlaceOrderResult> ExecuteAsync(OrderId orderId) =>
            ExecuteAsync(new PlaceOrderCommand(orderId));
    }
}
