namespace DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

/// <summary>
/// Who initiates the exchange with an external system.
/// </summary>
public enum Interaction
{
    /// <summary>
    /// This context initiates (API call, polling, file upload) — edge in <c>Adapter.Outgoing</c>.
    /// </summary>
    Outbound,

    /// <summary>
    /// The external system initiates (webhook, queue message, file drop) — edge in <c>Adapter.Incoming</c>.
    /// </summary>
    Inbound,
}
