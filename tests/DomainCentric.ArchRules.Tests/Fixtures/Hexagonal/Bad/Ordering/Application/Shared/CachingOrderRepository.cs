namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Application.Shared;

/// <summary>DCA-HEX-008: a *Repository class outside the outgoing adapter namespace.</summary>
public sealed class CachingOrderRepository
{
    public int Size { get; private set; }
}
