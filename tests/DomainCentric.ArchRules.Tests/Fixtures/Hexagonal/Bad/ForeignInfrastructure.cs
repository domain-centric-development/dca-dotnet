namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Foreign.Infrastructure { public class Client {} }
namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Foreign.Domain.Model { public class Model {} }
namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Adapter.Outgoing { public class ForeignInfrastructureAdapter { private readonly DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Foreign.Infrastructure.Client client = new(); } }
