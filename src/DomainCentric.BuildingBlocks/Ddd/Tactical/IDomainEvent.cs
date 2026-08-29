using System;

namespace DomainCentric.BuildingBlocks.Ddd.Tactical;

/// <summary>
/// Interface for Domain Events.
/// </summary>
/// <remarks>
/// <para>
/// Domain Events represent something that happened in the domain that domain experts care about.
/// They are internal to a bounded context and can evolve freely without versioning concerns.
/// </para>
/// <para><b>Characteristics:</b></para>
/// <list type="bullet">
///   <item><description>Immutable (sealed classes or records)</description></item>
///   <item><description>Named in the past tense (e.g., <c>ProductCreated</c>, <c>CartCleared</c>, <c>PriceChanged</c>)</description></item>
///   <item><description>Include timestamp, unique ID, and event-specific data</description></item>
///   <item><description>Carry no framework attributes or dependencies</description></item>
///   <item><description>Part of the Ubiquitous Language</description></item>
///   <item><description>Internal to a bounded context — no versioning needed</description></item>
/// </list>
/// <para><b>Use cases:</b></para>
/// <list type="bullet">
///   <item><description>Triggering side effects in other aggregates or contexts</description></item>
///   <item><description>Enabling eventual consistency between bounded contexts</description></item>
///   <item><description>Audit trail and event sourcing</description></item>
///   <item><description>Decoupling bounded contexts</description></item>
/// </list>
/// <para>
/// For events that cross bounded context boundaries, see <see cref="IIntegrationEvent"/>, which
/// adds versioning for backward compatibility.
/// </para>
/// <example>
/// <code>
/// public sealed record ProductCreated(
///     Guid EventId,
///     ProductId ProductId,
///     DateTimeOffset OccurredOn) : IDomainEvent
/// {
///     public static ProductCreated Now(ProductId productId) =>
///         new(Guid.NewGuid(), productId, DateTimeOffset.UtcNow);
/// }
/// </code>
/// </example>
/// <para><b>References:</b></para>
/// <list type="bullet">
///   <item><description>Eric Evans' Domain-Driven Design (2003)</description></item>
///   <item><description>Vaughn Vernon's Implementing Domain-Driven Design (2013), Chapter 8: "Domain Events"</description></item>
/// </list>
/// </remarks>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Timestamp when the event occurred in the domain.
    /// </summary>
    DateTimeOffset OccurredOn { get; }
}
