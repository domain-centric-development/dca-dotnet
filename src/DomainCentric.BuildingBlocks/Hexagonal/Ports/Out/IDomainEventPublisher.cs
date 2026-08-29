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
/// <b>Implementation note:</b> concrete implementations should ensure events are published only
/// after successful persistence to maintain data consistency.
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
    /// Call this after successfully persisting an aggregate. It publishes all collected events and
    /// then clears them to prevent duplicate publishing.
    /// </remarks>
    /// <param name="aggregate">The aggregate containing domain events.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when all events have been published and cleared.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="aggregate"/> is <see langword="null"/>.</exception>
    Task PublishAndClearEventsAsync(IAggregateRoot aggregate, CancellationToken cancellationToken = default);
}
