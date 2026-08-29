// Deliberate violations — one (or more) per DCA-ADV rule. See the class comments.
using System;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Advanced.Bad.Infrastructure.Stereotypes
{
    /// <summary>Stand-in for a DI/framework stereotype attribute (Spring's @Component).</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class ComponentAttribute : Attribute
    {
    }

    /// <summary>Stand-in for a DI/framework stereotype attribute (Spring's @Service).</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ServiceAttribute : Attribute
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Advanced.Bad.SharedKernel
{
    [DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships.SharedKernel]
    public static class SharedKernelContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Advanced.Bad.SharedKernel.Domain.Model
{
    public sealed record Money(decimal Amount) : IValue;
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Advanced.Bad.Order
{
    [BoundedContext("Order")]
    public static class OrderContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Advanced.Bad.Order.Domain.Model
{
    using DomainCentric.ArchRules.Tests.Fixtures.Advanced.Bad.Infrastructure.Stereotypes;
    using DomainCentric.ArchRules.Tests.Fixtures.Advanced.Bad.SharedKernel.Domain.Model;

    public readonly record struct OrderId(Guid Value) : IId;

    public sealed class Order : AggregateRootBase<Order, OrderId>
    {
        public Order(OrderId id, Money total)
        {
            Id = id;
            Total = total;
        }

        public override OrderId Id { get; }

        public Money Total { get; }
    }

    // DCA-ADV-001: domain event as a (sealed) class, not a record.
    public sealed class OrderShippedClassEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();

        public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    }

    // DCA-ADV-003 (and 001): domain event class that is neither a record nor sealed.
    public class OrderReopenedClassEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();

        public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    }

    // DCA-ADV-004: framework stereotype on a domain event.
    [Component]
    public sealed record OrderCancelled(Guid EventId, DateTimeOffset OccurredOn) : IDomainEvent;

    // DCA-ADV-007: version field on a pure domain event.
    public sealed record OrderPaid(Guid EventId, DateTimeOffset OccurredOn, int Version) : IDomainEvent;

    // DCA-ADV-008: no timestamp field — OccurredOn is computed, nothing is stored.
    public sealed record OrderArchived(Guid EventId) : IDomainEvent
    {
        public DateTimeOffset OccurredOn => DateTimeOffset.MinValue;
    }

    // DCA-ADV-009: domain service outside Domain.Service.
    public sealed class DiscountPolicy : IDomainService
    {
    }

    // DCA-ADV-013: IFactory implementer without 'Factory' suffix.
    public sealed class OrderBuilder : IFactory
    {
    }

    // DCA-ADV-015: framework stereotype on a factory.
    [Component]
    public sealed class ShipmentFactory : IFactory
    {
    }

    // DCA-ADV-016: stateful factory.
    public sealed class SequencedOrderFactory : IFactory
    {
        private long _sequence;

        public long Next() => ++_sequence;
    }

    // DCA-ADV-018: framework stereotype on a specification.
    [Component]
    public sealed class PaidOrderSpecification : ISpecification<Order>
    {
        public bool IsSatisfiedBy(Order candidate) => true;
    }

    public sealed class OrderFactory : IFactory
    {
        public Order Create(Money total) => new Order(new OrderId(Guid.NewGuid()), total);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Advanced.Bad.Order.Domain.Service
{
    using DomainCentric.ArchRules.Tests.Fixtures.Advanced.Bad.Infrastructure.Stereotypes;

    // DCA-ADV-012: stateful domain service (mutable field and settable auto-property).
    public sealed class OrderCounter : IDomainService
    {
        private int _count;

        public int LastSeen { get; set; }

        public void Increment() => _count++;
    }

    // DCA-ADV-011: framework stereotype on a domain service.
    [Service]
    public sealed class TaxService : IDomainService
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Advanced.Bad.Order.Events
{
    // DCA-ADV-005: integration event without [IntegrationEventType].
    public sealed record OrderCancelledEvent(Guid EventId, DateTimeOffset OccurredOn) : IIntegrationEvent;

    // DCA-ADV-006: version field duplicating the attribute.
    [IntegrationEventType("order.shipped", Version = 2)]
    public sealed record OrderShippedEvent(Guid EventId, DateTimeOffset OccurredOn, int Version) : IIntegrationEvent;
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Advanced.Bad.Order.Application.Misplaced
{
    using DomainCentric.ArchRules.Tests.Fixtures.Advanced.Bad.Order.Domain.Model;

    // DCA-ADV-002: domain event in the application layer.
    public sealed record OrderMisplaced(Guid EventId, DateTimeOffset OccurredOn) : IDomainEvent;

    // DCA-ADV-010 (and 009): domain service in the application layer.
    public sealed class ShippingCostService : IDomainService
    {
    }

    // DCA-ADV-014: factory in the application layer.
    public sealed class InvoiceFactory : IFactory
    {
    }

    // DCA-ADV-017: specification in the application layer.
    public sealed class OpenOrderSpecification : ISpecification<Order>
    {
        public bool IsSatisfiedBy(Order candidate) => true;
    }
}
