namespace DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Domain.Model;

// DCA-NAM-010: technical suffix in the domain
public sealed class OrderManager
{
    public Order Create(OrderId id) => Order.Place(id);
}

// DCA-NAM-008: a converter in the domain
public static class OrderConverter
{
    public static string ToText(Order order) => order.Id.Value.ToString();
}
