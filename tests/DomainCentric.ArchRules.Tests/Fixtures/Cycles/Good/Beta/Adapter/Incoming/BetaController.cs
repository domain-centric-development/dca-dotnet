using DomainCentric.ArchRules.Tests.Fixtures.Cycles.Good.Beta.Application.Shared;

namespace DomainCentric.ArchRules.Tests.Fixtures.Cycles.Good.Beta.Adapter.Incoming;

public sealed class BetaController
{
    private readonly BetaService _service;

    public BetaController(BetaService service)
    {
        _service = service;
    }

    public BetaService Service => _service;
}
