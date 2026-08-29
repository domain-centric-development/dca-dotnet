namespace DomainCentric.BuildingBlocks.Ddd.Tactical;

/// <summary>
/// Marker interface for Value Objects — immutable objects defined by their attributes, not by identity.
/// </summary>
/// <remarks>
/// <para>
/// Two value objects with the same attributes are interchangeable. They have no lifecycle and no
/// identifier; when a value must change, a new instance replaces the old one.
/// </para>
/// <para>
/// <b>Recommended implementation:</b> a <c>record</c> (or a <c>readonly record struct</c> for small,
/// allocation-sensitive values). Records give value-based equality and immutability with no boilerplate.
/// </para>
/// <example>
/// <code>
/// public sealed record Money(decimal Amount, Currency Currency) : IValue
/// {
///     public Money Add(Money other)
///     {
///         if (other.Currency != Currency)
///         {
///             throw new InvalidOperationException("Currency mismatch");
///         }
///         return this with { Amount = Amount + other.Amount };
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public interface IValue
{
}
