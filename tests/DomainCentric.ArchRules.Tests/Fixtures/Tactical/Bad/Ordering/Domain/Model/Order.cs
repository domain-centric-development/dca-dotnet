using DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Application.PlaceOrder;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Domain.Model;

/// <summary>DCA-TAC-002 (repository field), DCA-TAC-003 (field of another aggregate root).</summary>
public sealed class Order : AggregateRootBase<Order, OrderId>
{
    private readonly Customer _customer;
    private readonly IOrderRepository _repository;

    public Order(OrderId id, Customer customer, IOrderRepository repository)
    {
        Id = id;
        _customer = customer;
        _repository = repository;
    }

    public override OrderId Id { get; }

    public Customer Customer => _customer;

    public void Persist() => _repository.SaveAsync(this).GetAwaiter().GetResult();
}
