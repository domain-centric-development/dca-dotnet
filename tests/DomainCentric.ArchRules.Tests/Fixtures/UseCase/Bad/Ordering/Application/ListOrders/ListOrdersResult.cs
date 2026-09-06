using System.Collections.Generic;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.ListOrders;

// DCA-USE-015: an aggregate behind a generic argument, one inside a nested part record, an entity inside a
// part record declared next to the result, one shared in Application.Shared, an aggregate behind an array,
// inside a record struct and next to the type parameter of a generic part record
public sealed record ListOrdersResult(
    IReadOnlyList<Order> Orders,
    ListOrdersResult.OrderView Latest,
    LineView FirstLine,
    IReadOnlyList<OrderPart> Parts,
    Order[] Archive,
    LinePart Struct,
    Boxed<int> Boxed)
{
    public sealed record OrderView(Order Order, int LineCount);
}

public sealed record LineView(OrderLine Line);

public readonly record struct LinePart(Order Order);

public sealed record Boxed<T>(T Value, Order Extra);
