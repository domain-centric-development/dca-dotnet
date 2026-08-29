using System;

namespace DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

/// <summary>
/// Marks a type as an Open Host Service in Domain-Driven Design.
/// </summary>
/// <remarks>
/// <para>
/// An Open Host Service is a public API that a bounded context exposes for other bounded contexts to
/// consume. It acts as an incoming adapter that translates domain objects to DTOs, similar to how
/// REST controllers translate domain objects to JSON.
/// </para>
/// <para><b>Architectural rules:</b></para>
/// <list type="bullet">
///   <item><description>Open Host Services must be placed in <c>Adapter.Incoming.OpenHost</c></description></item>
///   <item><description>They must return DTOs, never domain objects</description></item>
///   <item><description>Outgoing adapters from other contexts may ONLY reference Open Host Services</description></item>
///   <item><description>Application layer use cases must NEVER reference Open Host Services directly</description></item>
/// </list>
/// <example>
/// <code>
/// [OpenHostService("Product Catalog", Description = "Provides product information for other contexts")]
/// public sealed class ProductCatalogService
/// {
///     // Returns DTOs, not domain objects
///     public Task&lt;ProductInfo?&gt; GetProductInfoAsync(ProductId id, CancellationToken cancellationToken) { /* ... */ }
/// }
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="Strategic.BoundedContextAttribute"/>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public sealed class OpenHostServiceAttribute : Attribute
{
    /// <summary>
    /// Declares an Open Host Service.
    /// </summary>
    /// <param name="context">The bounded context this Open Host Service belongs to.</param>
    public OpenHostServiceAttribute(string context)
    {
        Context = context;
    }

    /// <summary>
    /// The bounded context this Open Host Service belongs to.
    /// </summary>
    public string Context { get; }

    /// <summary>
    /// Description of what this Open Host Service provides. Defaults to empty.
    /// </summary>
    public string Description { get; set; } = "";
}
