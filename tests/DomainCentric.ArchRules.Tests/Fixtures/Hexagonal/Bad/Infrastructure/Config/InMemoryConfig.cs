using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Adapter.Outgoing;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Application.Shared;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Infrastructure.Config;

public static class InMemoryConfig
{
    public static IOrderRepository OrderRepository() => new InMemoryOrderRepository();
}
