namespace DomainCentric.BuildingBlocks.Ddd.Tactical;

/// <summary>
/// Interface for Aggregate Roots.
/// </summary>
/// <remarks>
/// <para>
/// An aggregate root is the entry point to an aggregate — a cluster of domain objects that are
/// treated as a single unit. The aggregate root is responsible for maintaining invariants within
/// the aggregate and for collecting domain events that occur during state changes.
/// </para>
/// <para>
/// <b>Domain Events:</b> aggregate roots collect domain events when state changes occur. These
/// events are published after the aggregate is persisted, enabling eventual consistency and loose
/// coupling between aggregates and bounded contexts.
/// </para>
/// <para><b>Usage pattern:</b></para>
/// <example>
/// <code>
/// // 1. Aggregate performs a business operation and raises an event
/// product.ChangePrice(newPrice);
///
/// // 2. Repository saves the aggregate
/// await productRepository.SaveAsync(product, cancellationToken);
///
/// // 3. Application layer publishes the events and clears them
/// await domainEventPublisher.PublishAndClearEventsAsync(product, cancellationToken);
/// </code>
/// </example>
/// <para>
/// The interface adds no members beyond <see cref="IEntity{TSelf, TId}"/> and
/// <see cref="IAggregateRoot"/>; it ties the two together with the concrete types.
/// </para>
/// </remarks>
/// <typeparam name="TSelf">The concrete aggregate root type itself.</typeparam>
/// <typeparam name="TId">The identifier type of the aggregate root.</typeparam>
public interface IAggregateRoot<TSelf, TId> : IEntity<TSelf, TId>, IAggregateRoot
    where TSelf : IAggregateRoot<TSelf, TId>
    where TId : IId
{
}
