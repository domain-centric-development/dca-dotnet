using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Domain.Model;

public sealed class Customer : AggregateRootBase<Customer, CustomerId>
{
    public Customer(CustomerId id)
    {
        Id = id;
    }

    public override CustomerId Id { get; }
}
