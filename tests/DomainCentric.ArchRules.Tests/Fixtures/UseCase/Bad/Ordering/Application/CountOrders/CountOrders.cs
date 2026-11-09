using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.CountOrders;

/// <summary>DCA-USE-022: the use case answers a bare number whose meaning lives in the caller.</summary>
public interface ICountOrdersInputPort : IUseCase<CountOrdersQuery, int>
{
}

public sealed record CountOrdersQuery(Guid OrderId);

/// <summary>DCA-USE-022: a bare flag, declared by the class itself.</summary>
public sealed class OrderExistsUseCase : IUseCase<CountOrdersQuery, bool?>
{
    private readonly IOrderRepository _orders;

    public OrderExistsUseCase(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<bool?> ExecuteAsync(CountOrdersQuery query, CancellationToken cancellationToken = default) =>
        await _orders.FindByIdAsync(new OrderId(query.OrderId), cancellationToken) is not null;
}

/// <summary>DCA-USE-021: the use case keeps what one call found for the next one.</summary>
public sealed class CountOrdersUseCase(IOrderRepository orders) : ICountOrdersInputPort
{
    private int _calls;

    public string? LastOrder { get; private set; }

    public async Task<int> ExecuteAsync(CountOrdersQuery query, CancellationToken cancellationToken = default)
    {
        _calls++;
        var order = await orders.FindByIdAsync(new OrderId(query.OrderId), cancellationToken);
        orders = orders ?? throw new InvalidOperationException();
        LastOrder = order?.Id.Value.ToString();
        return _calls;
    }
}
