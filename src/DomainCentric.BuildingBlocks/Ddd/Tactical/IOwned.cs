namespace DomainCentric.BuildingBlocks.Ddd.Tactical;

/// <summary>
/// Non-generic base of <see cref="IOwned{TOwnerId}"/>: an aggregate root that belongs to someone.
/// </summary>
/// <remarks>
/// It exists so that architecture rules can test assignability without closing the generic marker. Domain
/// code implements <see cref="IOwned{TOwnerId}"/>, never this interface directly.
/// </remarks>
public interface IOwned
{
}
