using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

/// <summary>
/// Outbound port for publishing domain events.
/// </summary>
/// <remarks>
/// <para>
/// This interface defines the contract for publishing domain events from the application layer to
/// the infrastructure layer, enabling loose coupling between aggregates and event handlers.
/// </para>
/// <para>
/// This is an outbound port (secondary/driven port in Hexagonal Architecture) used across all
/// bounded contexts, making it part of the Shared Kernel. The application layer depends on this
/// interface, while concrete implementations reside in the infrastructure layer, following the
/// Dependency Inversion Principle.
/// </para>
/// <example>
/// <code>
/// // In a use case, after saving the aggregate:
/// var saved = await productRepository.SaveAsync(product, cancellationToken);
/// await domainEventPublisher.PublishAndClearEventsAsync(saved, cancellationToken);
/// </code>
/// </example>
/// <para><b>Benefits:</b></para>
/// <list type="bullet">
///   <item><description>Application layer remains framework-independent</description></item>
///   <item><description>Events are published only after successful persistence</description></item>
///   <item><description>Enables asynchronous event handling</description></item>
///   <item><description>Supports eventual consistency across aggregates</description></item>
///   <item><description>Easy to swap implementations or mock for testing</description></item>
/// </list>
/// <para>
/// <b>Sequence of operations — save, dispatch, then clear.</b> The use case calls
/// <c>PublishAndClearEventsAsync</c> after <c>SaveAsync</c>, inside the same transaction, so an event is never
/// dispatched for state that was not persisted. The implementation dispatches the collected events first and
/// clears the aggregate <em>afterwards</em>: clearing is the acknowledgement that every listener has seen the
/// event. A listener that throws therefore fails the use case and leaves the events on the aggregate — nothing is
/// silently lost. Clearing before dispatch would drop events on the first failing listener.
/// </para>
/// <para>
/// Integration events derived from these domain events (by an outgoing event adapter listening in-process) are
/// recorded in a transactional outbox inside the same transaction and delivered after commit, at least once;
/// their consumers are idempotent.
/// </para>
/// </remarks>
public interface IDomainEventPublisher : IOutputPort
{
    /// <summary>
    /// Publishes a single domain event.
    /// </summary>
    /// <remarks>
    /// The event is published to the underlying event infrastructure (in-process dispatcher,
    /// message broker, etc.).
    /// </remarks>
    /// <param name="event">The domain event to publish.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the event has been handed to the infrastructure.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="event"/> is <see langword="null"/>.</exception>
    Task PublishAsync(IDomainEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes all domain events from an aggregate and clears them.
    /// </summary>
    /// <remarks>
    /// Call after successfully persisting the aggregate, inside the transaction. Dispatches all collected events
    /// to their listeners and, once every listener completed, clears them from the aggregate — the clear is the
    /// acknowledgement. If a listener throws, the exception propagates, the events stay on the aggregate and the
    /// surrounding transaction rolls back.
    /// </remarks>
    /// <param name="aggregate">The aggregate containing domain events.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when all events have been published and cleared.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="aggregate"/> is <see langword="null"/>.</exception>
    Task PublishAndClearEventsAsync(IAggregateRoot aggregate, CancellationToken cancellationToken = default);
}
