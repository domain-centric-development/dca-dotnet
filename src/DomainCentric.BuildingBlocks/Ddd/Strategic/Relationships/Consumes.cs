namespace DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

/// <summary>
/// Published interface of the upstream that the downstream consumes.
/// </summary>
public enum Consumes
{
    /// <summary>
    /// The upstream's <c>Api</c> published interface (Open Host Service).
    /// </summary>
    Api,

    /// <summary>
    /// The upstream's <c>Events</c> published interface (integration events, trigger contracts).
    /// </summary>
    Events,
}
