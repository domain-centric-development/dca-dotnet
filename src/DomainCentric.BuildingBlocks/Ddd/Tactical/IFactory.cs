namespace DomainCentric.BuildingBlocks.Ddd.Tactical;

/// <summary>
/// Marker interface for Factories.
/// </summary>
/// <remarks>
/// <para>
/// Factories encapsulate complex object creation logic, particularly for Aggregates and Entities.
/// They ensure invariants are maintained from the moment of creation and hide complex construction
/// details.
/// </para>
/// <para><b>When to use:</b></para>
/// <list type="bullet">
///   <item><description>Object construction is complex and involves multiple steps</description></item>
///   <item><description>Creation requires knowledge not belonging to the object itself</description></item>
///   <item><description>The constructor would violate the object's invariants</description></item>
///   <item><description>Different configurations of the same type must be created</description></item>
/// </list>
/// <para><b>Characteristics:</b></para>
/// <list type="bullet">
///   <item><description>Stateless or with minimal state</description></item>
///   <item><description>Return fully formed, valid objects</description></item>
///   <item><description>Carry no framework attributes or dependencies</description></item>
///   <item><description>Part of the domain model</description></item>
/// </list>
/// <example>
/// <code>
/// public sealed class ProductFactory : IFactory
/// {
///     public Product CreateProduct(ProductName name, Sku sku, Price price)
///     {
///         // Complex construction logic and validation
///         return new Product(/* ... */);
///     }
/// }
/// </code>
/// </example>
/// <para>
/// <b>Alternative:</b> for simple cases, static factory methods on the domain object itself
/// (e.g., <c>Product.Of(...)</c>) are preferred over separate Factory classes.
/// </para>
/// <para>
/// <b>Reference:</b> Eric Evans' Domain-Driven Design (2003), Chapter 6: "The Life Cycle of a Domain Object".
/// </para>
/// </remarks>
public interface IFactory
{
}
