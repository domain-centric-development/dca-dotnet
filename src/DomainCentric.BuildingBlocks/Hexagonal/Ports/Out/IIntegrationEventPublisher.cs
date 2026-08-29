using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

/// <summary>
/// Outbound port for publishing integration events across bounded-context boundaries.
/// </summary>
/// <remarks>
/// <para>
/// Use cases publish boundary-crossing facts through this port; the implementation (e.g. a
/// transactional-outbox adapter) decides how the event becomes durable and reaches external
/// consumers. This keeps the application layer free of delivery concerns.
/// </para>
/// <para>
/// Distinct from <see cref="IDomainEventPublisher"/>: that port publishes in-context
/// <see cref="IDomainEvent"/>s after persistence; this one publishes the versioned, serializable
/// <see cref="IIntegrationEvent"/> contract to other contexts or systems.
/// </para>
/// <para>
/// Note the two-level port distinction of the outbox subsystem: this interface is an
/// <b>application output port</b> (used by use cases) and therefore carries the
/// <see cref="IOutputPort"/> marker. The outbox <em>store</em> behind it is an internal port of the
/// outbox adapter subsystem — no use case depends on it, so it deliberately carries no marker.
/// </para>
/// </remarks>
/// <seealso cref="IDomainEventPublisher"/>
/// <seealso cref="IIntegrationEvent"/>
public interface IIntegrationEventPublisher : IOutputPort
{
    /// <summary>
    /// Publishes an integration event for delivery beyond this bounded context.
    /// </summary>
    /// <param name="event">The integration event to publish.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the event has been accepted for delivery.</returns>
    Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default);
}
