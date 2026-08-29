namespace DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

/// <summary>
/// Implementation status of a declared relationship.
/// </summary>
public enum UpstreamStatus
{
    /// <summary>
    /// The dependency exists in code; the architecture rules require at least one real edge.
    /// </summary>
    Implemented,

    /// <summary>
    /// The relationship is intended but has no code edge yet; rendered as planned in the context map.
    /// </summary>
    Planned,
}
