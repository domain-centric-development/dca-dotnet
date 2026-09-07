using System.Collections.Generic;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.ListOrders;

/// <summary>
/// A generic base without the <c>Result</c> suffix: a result that derives from it decides what <c>T</c> is, so the
/// inherited member must be read in the subclass's context. <c>Count</c> uses no type parameter.
/// </summary>
public abstract class GenericBase<T>
{
    protected GenericBase(T value, int count)
    {
        Value = value;
        Count = count;
    }

    public T Value { get; }

    public int Count { get; }
}

/// <summary>Binds the base's <c>T</c> to <c>IReadOnlyList&lt;U&gt;</c> and leaves <c>U</c> to the next subclass.</summary>
public abstract class Intermediate<U> : GenericBase<IReadOnlyList<U>>
{
    protected Intermediate(IReadOnlyList<U> value, int count) : base(value, count)
    {
    }
}

// DCA-USE-015: the aggregate arrives through the inherited member 'T Value', with T bound to Order
public sealed class GenericOrderResult : GenericBase<Order>
{
    public GenericOrderResult(Order order) : base(order, 1)
    {
    }
}

// DCA-USE-015: two levels of inheritance - T = IReadOnlyList<U>, U = Order
public sealed class BatchedOrdersResult : Intermediate<Order>
{
    public BatchedOrdersResult(IReadOnlyList<Order> orders) : base(orders, orders.Count)
    {
    }
}

// DCA-USE-015: a nested container bound to the inherited type parameter
public sealed class OrdersByRegionResult : GenericBase<IReadOnlyDictionary<string, IReadOnlyList<Order>>>
{
    public OrdersByRegionResult(IReadOnlyDictionary<string, IReadOnlyList<Order>> byRegion) : base(byRegion, byRegion.Count)
    {
    }
}

// DCA-USE-015: an entity, not an aggregate root, bound to the inherited type parameter
public sealed class LineItemResult : GenericBase<OrderLine>
{
    public LineItemResult(OrderLine line) : base(line, 1)
    {
    }
}
