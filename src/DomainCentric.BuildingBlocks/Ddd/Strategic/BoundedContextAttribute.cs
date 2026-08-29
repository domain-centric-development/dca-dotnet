using System;

namespace DomainCentric.BuildingBlocks.Ddd.Strategic;

/// <summary>
/// Marks a namespace as a Bounded Context in Domain-Driven Design.
/// </summary>
/// <remarks>
/// <para>
/// A Bounded Context is an explicit boundary within which a domain model exists. Each Bounded
/// Context has its own Ubiquitous Language and should be isolated from other contexts.
/// </para>
/// <para>
/// <b>Usage (.NET convention):</b> C# has no namespace-level attributes. Place this attribute on
/// exactly one class that resides directly in the context's root namespace — a small, empty,
/// static <i>context marker class</i>. Architecture rules and tooling discover a bounded context by
/// finding such a class; the namespace of that class is the context's root namespace, and every
/// type in it or below it belongs to the context.
/// </para>
/// <example>
/// <code>
/// namespace Acme.Shop.Cart;
///
/// [BoundedContext("Shopping Cart", Description = "Carts and their items before checkout")]
/// public static class CartContext
/// {
/// }
/// </code>
/// </example>
/// <para><b>Architectural rules:</b></para>
/// <list type="bullet">
///   <item><description>Bounded contexts must not directly depend on each other (except via Shared Kernel or events)</description></item>
///   <item><description>Each bounded context has its own domain, application, and adapter layers</description></item>
///   <item><description>Cross-context communication should use domain events or Anti-Corruption Layers</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Relationships.SharedKernelAttribute"/>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class BoundedContextAttribute : Attribute
{
    /// <summary>
    /// Declares a bounded context.
    /// </summary>
    /// <param name="name">The name of the bounded context (e.g., "Product Catalog", "Shopping Cart").</param>
    public BoundedContextAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// The name of the bounded context (e.g., "Product Catalog", "Shopping Cart").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// An optional description of the bounded context's responsibility. Defaults to empty.
    /// </summary>
    public string Description { get; set; } = "";
}
