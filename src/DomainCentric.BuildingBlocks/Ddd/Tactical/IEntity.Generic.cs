namespace DomainCentric.BuildingBlocks.Ddd.Tactical;

/// <summary>
/// Interface for Entities — domain objects distinguished by identity rather than by attributes.
/// </summary>
/// <remarks>
/// <para>
/// An entity has a lifecycle: its attributes may change over time, yet it stays the same thing
/// because its <see cref="Id"/> does not change. Two entities are the same entity exactly when they
/// have the same identifier; see <see cref="SameIdentityAs"/>.
/// </para>
/// <para>
/// The self-referencing type parameter <typeparamref name="TSelf"/> (curiously recurring pattern)
/// lets <see cref="SameIdentityAs"/> accept only entities of the same concrete type, so that a
/// <c>Product</c> can never be compared to an <c>Order</c> by accident.
/// </para>
/// <example>
/// <code>
/// public sealed class CartItem : IEntity&lt;CartItem, CartItemId&gt;
/// {
///     public CartItemId Id { get; }
///     public Quantity Quantity { get; private set; }
///
///     public CartItem(CartItemId id, Quantity quantity)
///     {
///         Id = id;
///         Quantity = quantity;
///     }
///
///     public void ChangeQuantity(Quantity quantity) => Quantity = quantity;
/// }
/// </code>
/// </example>
/// </remarks>
/// <typeparam name="TSelf">The concrete entity type itself.</typeparam>
/// <typeparam name="TId">The identifier type of the entity.</typeparam>
public interface IEntity<TSelf, TId> : IEntity
    where TSelf : IEntity<TSelf, TId>
    where TId : IId
{
    /// <summary>
    /// The identifier of this entity. It never changes during the entity's lifetime.
    /// </summary>
    TId Id { get; }

    /// <summary>
    /// Tests whether this entity and <paramref name="other"/> are the same entity, i.e. carry the same identifier.
    /// </summary>
    /// <param name="other">The entity to compare with; may be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="other"/> is not <see langword="null"/> and has the same <see cref="Id"/>.</returns>
    bool SameIdentityAs(TSelf other) => other is not null && Id.Equals(other.Id);
}
