using Microsoft.AspNetCore.Mvc;

namespace DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Adapter.Incoming.Rest;

// DCA-NAM-006: [ApiController] not ending with the REST controller suffix ('Controller')
[ApiController]
public sealed class OrderResource : ControllerBase
{
}
