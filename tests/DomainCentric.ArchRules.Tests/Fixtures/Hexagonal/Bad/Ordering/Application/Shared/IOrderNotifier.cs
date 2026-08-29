namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Application.Shared;

/// <summary>DCA-HEX-009: top-level interface in Application.Shared that does not extend IOutputPort.</summary>
public interface IOrderNotifier
{
    void Notify(string message);
}
