using System;

namespace DomainCentric.BuildingBlocks.Ddd.Tactical;

/// <summary>
/// Marker interface for Integration Events — adapter-layer DTOs published across bounded contexts.
/// </summary>
/// <remarks>
/// <para>
/// Integration Events are <b>not</b> domain events. They are adapter-layer data transfer objects
/// created when an outgoing event adapter consumes an internal <see cref="IDomainEvent"/> and
/// publishes a cross-context representation. This separation acts as an Anti-Corruption Layer (ACL)
/// between the publishing context's domain model and external consumers.
/// </para>
/// <para><b>Key differences from Domain Events:</b></para>
/// <list type="bullet">
///   <item><description><b>Layer:</b> adapter layer (<c>Adapter.Outgoing.Event</c>), not domain layer</description></item>
///   <item><description><b>Purpose:</b> cross bounded context communication (domain events are internal)</description></item>
///   <item><description><b>Versioning:</b> strict backward compatibility required (domain events can change freely).
///   The schema version is a <b>type property</b>, declared via <see cref="IntegrationEventTypeAttribute"/>,
///   never a data field on the event instance.</description></item>
///   <item><description><b>Naming:</b> suffixed with <c>Event</c> (e.g., <c>CartCheckedOutEvent</c>), while
///   domain events have no suffix (e.g., <c>CartCheckedOut</c>)</description></item>
///   <item><description><b>Creation:</b> created by outgoing event adapters via <c>From(domainEvent)</c> factory methods</description></item>
///   <item><description><b>Consumption:</b> consumed by incoming event adapters in other contexts</description></item>
/// </list>
/// <para><b>Event flow:</b></para>
/// <code>
/// Aggregate raises DomainEvent
///   → Outgoing event publisher adapter listens
///     → Creates IntegrationEvent via From() factory
///       → Publishes IntegrationEvent
///         → Incoming event consumer in other context receives it
/// </code>
/// <example>
/// <code>
/// // In Cart.Adapter.Outgoing.Event
/// [IntegrationEventType("cart-checked-out", Version = 1)]
/// public sealed record CartCheckedOutEvent(
///     Guid EventId,
///     CartId CartId,
///     CustomerId CustomerId,
///     Money TotalAmount,
///     int ItemCount,
///     IReadOnlyList&lt;CartCheckedOutEvent.ItemInfo&gt; Items,
///     DateTimeOffset OccurredOn) : IIntegrationEvent
/// {
///     public sealed record ItemInfo(ProductId ProductId, int Quantity);
///
///     public static CartCheckedOutEvent From(CartCheckedOut domainEvent)
///     {
///         var items = domainEvent.Items
///             .Select(i => new ItemInfo(i.ProductId, i.Quantity))
///             .ToList();
///         return new CartCheckedOutEvent(
///             domainEvent.EventId, domainEvent.CartId, domainEvent.CustomerId,
///             domainEvent.TotalAmount, items.Count, items, domainEvent.OccurredOn);
///     }
/// }
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="IDomainEvent"/>
public interface IIntegrationEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    DateTimeOffset OccurredOn { get; }
}
