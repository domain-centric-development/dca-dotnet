namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Domain.Model;

/// <summary>Not an Entity itself; its inherited public setter is only visible through the inherited members.</summary>
public abstract class TrackedEntity
{
    public string? Note { get; set; }

    public void SetTracking(string tracking) => Note = tracking;
}
