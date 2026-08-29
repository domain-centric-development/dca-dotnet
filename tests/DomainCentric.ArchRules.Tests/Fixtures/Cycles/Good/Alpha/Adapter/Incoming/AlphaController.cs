using DomainCentric.ArchRules.Tests.Fixtures.Cycles.Good.Alpha.Application.Shared;

namespace DomainCentric.ArchRules.Tests.Fixtures.Cycles.Good.Alpha.Adapter.Incoming;

public sealed class AlphaController
{
    private readonly AlphaService _service;

    public AlphaController(AlphaService service)
    {
        _service = service;
    }

    public AlphaService Service => _service;
}
