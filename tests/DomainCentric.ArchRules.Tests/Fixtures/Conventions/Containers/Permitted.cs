namespace DomainCentric.ArchRules.Tests.Fixtures.Conventions.Containers.Ordering.Application.UseCases.PlaceOrder {
 public interface ILocalRepository : DomainCentric.BuildingBlocks.Hexagonal.Ports.Out.IRepository {}
 public interface ILocalStore : DomainCentric.BuildingBlocks.Hexagonal.Ports.Out.IStore { object FindById(string id); }
}
namespace DomainCentric.ArchRules.Tests.Fixtures.Conventions.Containers.Ordering.Adapter.Outgoing { public record ProviderResponse(string Body); }
namespace DomainCentric.ArchRules.Tests.Fixtures.Conventions.Containers.Ordering.Domain { public class PortfolioManager {} }
