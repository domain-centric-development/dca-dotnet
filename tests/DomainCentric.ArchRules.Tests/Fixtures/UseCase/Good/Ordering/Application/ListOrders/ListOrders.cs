using System;
using System.Collections.Generic;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Application.ListOrders;

public interface IListOrdersInputPort : IUseCase<ListOrdersQuery, ListOrdersResult>
{
}

public sealed record ListOrdersQuery(Guid CustomerId);

// DCA-USE-015: ids, values, a read model, nested, same-namespace and shared parts, an array and a generic part
// over values — no aggregate, no entity
public sealed record ListOrdersResult(
    IReadOnlyList<OrderSummary> Orders,
    IReadOnlyList<ListOrdersResult.OrderLine> Lines,
    OrderTotals Totals,
    Money? Credit,
    IReadOnlyList<OrderPart> Parts,
    Money[] Refunds,
    Boxed<Money> Deposit)
{
    public sealed record OrderLine(OrderId OrderId, Money Price, int Quantity);
}

public sealed record OrderTotals(Money Gross, Money Tax);

public sealed record Boxed<T>(T Value, Money Fee);
