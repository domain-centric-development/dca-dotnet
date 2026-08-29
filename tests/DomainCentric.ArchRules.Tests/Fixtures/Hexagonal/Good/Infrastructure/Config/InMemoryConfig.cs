using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Ordering.Adapter.Outgoing;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Ordering.Application.Shared;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Infrastructure.Config;

public static class InMemoryConfig
{
    public static IOrderRepository OrderRepository() => new InMemoryOrderRepository();
}
