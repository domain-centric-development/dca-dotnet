using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.Shared;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.ShowOrderByText;

/// <summary>DCA-USE-020: the application parses raw text into an id - in an async body and in a helper.</summary>
public sealed class ShowOrderByTextUseCase
{
    private readonly IOrderRepository _orders;

    public ShowOrderByTextUseCase(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<bool> ExistsAsync(string orderId, CancellationToken cancellationToken = default) =>
        await _orders.FindByIdAsync(new OrderId(Guid.Parse(orderId)), cancellationToken) is not null;
}

public static class OrderIdText
{
    public static OrderId? Read(string text) => Guid.TryParse(text, out var value) ? new OrderId(value) : null;
}
