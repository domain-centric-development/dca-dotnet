using System.Collections.Generic;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.ListOrders;

// DCA-USE-015: an aggregate behind a generic argument, one inside a nested part record, an entity inside a
// part record declared next to the result, one shared in Application.Shared, an aggregate behind an array,
// inside a record struct and next to the type parameter of a generic part record; the same part record
// appears twice, and both paths are reported
public sealed record ListOrdersResult(
    IReadOnlyList<Order> Orders,
    ListOrdersResult.OrderView Latest,
    LineView FirstLine,
    LineView LastLine,
    IReadOnlyList<OrderPart> Parts,
    Order[] Archive,
    LinePart Struct,
    Boxed<int> Boxed)
{
    public sealed record OrderView(Order Order, int LineCount);
}

/// <summary>Not a result itself (no suffix), but a result inherits its aggregate property.</summary>
public abstract class BaseListing
{
    protected BaseListing(Order pinned) => Pinned = pinned;

    public Order Pinned { get; }
}

// DCA-USE-015: the aggregate arrives through the inherited property of a base class without suffix
public sealed class ArchivedOrdersResult : BaseListing
{
    public ArchivedOrdersResult(Order pinned, int count) : base(pinned) => Count = count;

    public int Count { get; }
}

public sealed record LineView(OrderLine Line);

public readonly record struct LinePart(Order Order);

public sealed record Boxed<T>(T Value, Order Extra);
