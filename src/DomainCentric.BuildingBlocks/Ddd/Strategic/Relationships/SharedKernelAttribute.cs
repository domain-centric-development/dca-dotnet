using System;

namespace DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

/// <summary>
/// Marks a namespace as the Shared Kernel in Domain-Driven Design.
/// </summary>
/// <remarks>
/// <para>
/// A Shared Kernel is a small, carefully curated subset of the domain model that is shared between
/// bounded contexts. Changes to the Shared Kernel require coordination between all teams that use it.
/// </para>
/// <para>
/// <b>Usage (.NET convention):</b> place this attribute on one class that resides directly in the
/// shared kernel's root namespace — the same marker-class convention as
/// <see cref="Strategic.BoundedContextAttribute"/>.
/// </para>
/// <example>
/// <code>
/// namespace Acme.Shop.SharedKernel;
///
/// [SharedKernel(Description = "Common value objects and cross-context identifiers")]
/// public static class SharedKernelContext
/// {
/// }
/// </code>
/// </example>
/// <para><b>What belongs in the Shared Kernel:</b></para>
/// <list type="bullet">
///   <item><description>Universal value objects (Money, Currency)</description></item>
///   <item><description>Cross-context identifiers (ProductId, UserId)</description></item>
///   <item><description>Common domain primitives</description></item>
/// </list>
/// <para><b>What does NOT belong in the Shared Kernel:</b></para>
/// <list type="bullet">
///   <item><description>Aggregates (each belongs to one context)</description></item>
///   <item><description>Context-specific business logic</description></item>
///   <item><description>Infrastructure concerns</description></item>
/// </list>
/// </remarks>
/// <seealso cref="Strategic.BoundedContextAttribute"/>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SharedKernelAttribute : Attribute
{
    /// <summary>
    /// An optional description of the shared kernel's contents. Defaults to empty.
    /// </summary>
    public string Description { get; set; } = "";
}
