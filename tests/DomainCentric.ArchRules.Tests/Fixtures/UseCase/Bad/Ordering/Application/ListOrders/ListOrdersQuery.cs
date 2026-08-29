namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.ListOrders;

// DCA-USE-005: mutable, non-sealed query
public class ListOrdersQuery
{
    public int Page { get; set; }
}
