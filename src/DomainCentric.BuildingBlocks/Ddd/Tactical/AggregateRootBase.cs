using System;
using System.Collections.Generic;

namespace DomainCentric.BuildingBlocks.Ddd.Tactical;

/// <summary>
/// Abstract base class for Aggregate Roots providing domain event collection.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a reusable implementation of domain event collection that aggregate roots
/// can use by deriving from it. It handles the storage and management of domain events that occur
/// during aggregate state changes.
/// </para>
/// <example>
/// <code>
/// public sealed class Product : AggregateRootBase&lt;Product, ProductId&gt;
/// {
///     public override ProductId Id { get; }
///     public Price Price { get; private set; }
///
///     public void ChangePrice(Price newPrice)
///     {
///         var oldPrice = Price;
///         Price = newPrice;
///
///         // Raise domain event
///         RegisterEvent(new ProductPriceChanged(Guid.NewGuid(), Id, oldPrice, newPrice, DateTimeOffset.UtcNow));
///     }
/// }
/// </code>
/// </example>
/// </remarks>
/// <typeparam name="TSelf">The concrete aggregate root type itself.</typeparam>
/// <typeparam name="TId">The identifier type of the aggregate root.</typeparam>
public abstract class AggregateRootBase<TSelf, TId> : IAggregateRoot<TSelf, TId>
    where TSelf : IAggregateRoot<TSelf, TId>
    where TId : IId
{
    private readonly List<IDomainEvent> _domainEvents = new List<IDomainEvent>();

    /// <inheritdoc />
    public abstract TId Id { get; }

    /// <inheritdoc />
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Registers a domain event to be published after the aggregate is persisted.
    /// </summary>
    /// <remarks>
    /// Call this method from within the aggregate's business methods when something significant
    /// happens that other parts of the system might care about.
    /// </remarks>
    /// <param name="event">The domain event to register.</param>
    /// <exception cref="ArgumentNullException"><paramref name="event"/> is <see langword="null"/>.</exception>
    protected void RegisterEvent(IDomainEvent @event)
    {
        if (@event is null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        _domainEvents.Add(@event);
    }
}
