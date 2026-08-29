namespace DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

/// <summary>
/// Translation strategy of the downstream context.
/// </summary>
public enum Translation
{
    /// <summary>
    /// Caller-owned port plus an adapter that translates upstream contract types into the
    /// downstream's own model. Upstream types never leave the adapter.
    /// </summary>
    AntiCorruptionLayer,

    /// <summary>
    /// Upstream contract types are used directly; the downstream conforms to the upstream's
    /// published language. Still forbidden in the domain layer.
    /// </summary>
    Conformist,
}
