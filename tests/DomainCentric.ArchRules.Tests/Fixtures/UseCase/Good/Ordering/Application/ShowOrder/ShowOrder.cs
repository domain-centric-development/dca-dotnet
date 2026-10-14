using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Application.ShowOrder;

public interface IShowOrderInputPort : IUseCase<ShowOrderQuery, ShowOrderResult>
{
}

public sealed record ShowOrderQuery(Guid OrderId);

public sealed record ShowOrderResult(Guid? OrderId);

/// <summary>DCA-USE-019: a query loads and answers none when nothing exists.</summary>
public sealed class ShowOrderUseCase : IShowOrderInputPort
{
    private readonly IOrderRepository _orders;

    public ShowOrderUseCase(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<ShowOrderResult> ExecuteAsync(ShowOrderQuery query, CancellationToken cancellationToken = default)
    {
        var order = await _orders.FindByIdAsync(new OrderId(query.OrderId), cancellationToken);
        return new ShowOrderResult(order?.Id.Value);
    }
}
