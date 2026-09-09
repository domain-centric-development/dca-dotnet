namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Application.PlaceOrder;
public class DirectConstruction {
 public object Execute() => new DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Domain.Model.Shipment("x", null!);
}
