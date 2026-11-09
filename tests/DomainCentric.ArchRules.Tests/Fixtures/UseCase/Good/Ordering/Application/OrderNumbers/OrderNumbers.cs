using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Application.OrderNumbers;

/// <summary>DCA-USE-022: a container of text is a result shape of its own and passes.</summary>
public interface IOrderNumbersInputPort : IUseCase<OrderNumbersQuery, IReadOnlyList<string>>
{
}

public sealed record OrderNumbersQuery(Guid OrderId);

/// <summary>DCA-USE-021: a captured primary-constructor parameter that nothing reassigns is wiring, not state.</summary>
public sealed class OrderNumbersUseCase(IOrderRepository orders) : IOrderNumbersInputPort
{
    public async Task<IReadOnlyList<string>> ExecuteAsync(OrderNumbersQuery query, CancellationToken cancellationToken = default)
    {
        var order = await orders.FindByIdAsync(new OrderId(query.OrderId), cancellationToken);
        return order is null ? Array.Empty<string>() : new[] { order.Id.Value.ToString() };
    }
}
