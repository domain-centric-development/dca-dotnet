using System.Collections.Generic;

namespace DomainCentric.BuildingBlocks.Ddd.Tactical;

/// <summary>
/// Non-generic base for Aggregate Roots: the domain-event collection contract.
/// </summary>
/// <remarks>
/// <para>
/// This interface carries the members that do not depend on the aggregate's concrete type or
/// identifier type, so that infrastructure such as an <c>IDomainEventPublisher</c> can accept any
/// aggregate root, and so that architecture rules can test assignability without closing the
/// generic <see cref="IAggregateRoot{TSelf, TId}"/>.
/// </para>
/// <para>
/// Domain code should implement <see cref="IAggregateRoot{TSelf, TId}"/> (or derive from
/// <see cref="AggregateRootBase{TSelf, TId}"/>), never this interface directly.
/// </para>
/// </remarks>
public interface IAggregateRoot : IEntity
{
    /// <summary>
    /// The domain events that occurred during state changes since the events were last cleared.
    /// </summary>
    /// <remarks>
    /// These events should be published after the aggregate is successfully persisted and then cleared.
    /// </remarks>
    /// <value>A read-only list of domain events; never <see langword="null"/>, possibly empty.</value>
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all collected domain events.
    /// </summary>
    /// <remarks>
    /// Call this after the events have been successfully published to prevent duplicate publishing.
    /// </remarks>
    void ClearDomainEvents();
}
