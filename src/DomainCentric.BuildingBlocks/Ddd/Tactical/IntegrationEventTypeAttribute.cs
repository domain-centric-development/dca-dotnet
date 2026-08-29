using System;

namespace DomainCentric.BuildingBlocks.Ddd.Tactical;

/// <summary>
/// The mandatory contract identity every <see cref="IIntegrationEvent"/> carries: a stable logical
/// type name plus schema version, decoupled from the .NET type name.
/// </summary>
/// <remarks>
/// <para>
/// Decoupling name and version from the type means a breaking schema change ships as a new V2 type
/// that keeps the old logical <see cref="Name"/> in its attribute. A serializer keys
/// <c>(name, version)</c> to the type and stamps both onto the wire envelope, so a remote consumer
/// (outbox relay or message broker) picks its translator from the message alone.
/// </para>
/// <para>
/// This attribute is the <b>single source of truth for an event's version</b> — integration events
/// carry no <c>Version</c> data field on the instance.
/// </para>
/// <example>
/// <code>
/// [IntegrationEventType("cart-checked-out", Version = 1)]
/// public sealed record CartCheckedOutEvent(Guid EventId, DateTimeOffset OccurredOn /* ... */)
///     : IIntegrationEvent;
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="IIntegrationEvent"/>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class IntegrationEventTypeAttribute : Attribute
{
    /// <summary>
    /// Declares the contract identity of an integration event type.
    /// </summary>
    /// <param name="name">Stable logical type name of the event, independent of the .NET type name.</param>
    public IntegrationEventTypeAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Stable logical type name of the event, independent of the .NET type name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Schema version of the event contract; bump on breaking changes. Defaults to <c>1</c>.
    /// </summary>
    public int Version { get; set; } = 1;
}
