namespace DomainCentric.BuildingBlocks.Ddd.Tactical;

/// <summary>
/// Interface for Specifications — business rules as first-class objects.
/// </summary>
/// <remarks>
/// <para>
/// Specifications test whether an object satisfies certain criteria and can be combined to create
/// complex business rules.
/// </para>
/// <para><b>Use cases:</b></para>
/// <list type="bullet">
///   <item><description>Validating whether an object meets certain criteria</description></item>
///   <item><description>Selecting objects from a collection</description></item>
///   <item><description>Specifying how to create objects that fulfill requirements</description></item>
/// </list>
/// <para><b>Characteristics:</b></para>
/// <list type="bullet">
///   <item><description>Immutable value objects</description></item>
///   <item><description>Combinable (AND, OR, NOT operations)</description></item>
///   <item><description>Express business rules in the Ubiquitous Language</description></item>
///   <item><description>Carry no framework attributes or dependencies</description></item>
///   <item><description>Part of the domain model</description></item>
/// </list>
/// <example>
/// <code>
/// public sealed class ProductAvailabilitySpecification : ISpecification&lt;Product&gt;
/// {
///     public bool IsSatisfiedBy(Product candidate) => candidate.IsAvailable;
///
///     public ISpecification&lt;Product&gt; And(ISpecification&lt;Product&gt; other) =>
///         new AndSpecification&lt;Product&gt;(this, other);
/// }
/// </code>
/// </example>
/// <para><b>References:</b></para>
/// <list type="bullet">
///   <item><description>Eric Evans' Domain-Driven Design (2003), Chapter 9: "Specification"</description></item>
///   <item><description>Martin Fowler, Specifications pattern (https://martinfowler.com/apsupp/spec.pdf)</description></item>
/// </list>
/// </remarks>
/// <typeparam name="T">The type of candidate the specification evaluates.</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Tests whether <paramref name="candidate"/> satisfies this specification.
    /// </summary>
    /// <param name="candidate">The object to evaluate.</param>
    /// <returns><see langword="true"/> if the candidate satisfies the specification.</returns>
    bool IsSatisfiedBy(T candidate);
}
