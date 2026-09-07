using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Application.ListOrders;

/// <summary>A generic base without the <c>Result</c> suffix; results below bind <c>T</c> to values.</summary>
public abstract class GenericBase<T>
{
    protected GenericBase(T value) => Value = value;

    public T Value { get; }
}

/// <summary>Declares a type parameter that no instance member uses.</summary>
public abstract class UnusedParameterBase<T>
{
    protected UnusedParameterBase(int count) => Count = count;

    public int Count { get; }
}

// DCA-USE-015: the inherited type parameter bound to a string is a value
public sealed class OrderNameResult : GenericBase<string>
{
    public OrderNameResult(string name) : base(name)
    {
    }
}

// DCA-USE-015: the inherited type parameter bound to an id is a value
public sealed class OrderIdResult : GenericBase<OrderId>
{
    public OrderIdResult(OrderId id) : base(id)
    {
    }
}

// DCA-USE-015: the inherited type parameter bound to a value object
public sealed class OrderTotalResult : GenericBase<Money>
{
    public OrderTotalResult(Money total) : base(total)
    {
    }
}

// DCA-USE-015: the base binds T to an aggregate, but no instance member uses T - nothing is exposed
public sealed class OrderCountResult : UnusedParameterBase<Order>
{
    public OrderCountResult(int count) : base(count)
    {
    }
}
