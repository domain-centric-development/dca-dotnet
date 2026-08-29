using System;

namespace DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

/// <summary>
/// Declares a Partnership between two bounded contexts: both teams coordinate the evolution of a
/// shared contract and succeed or fail together on it.
/// </summary>
/// <remarks>
/// <para>
/// A partnership is a <em>governance</em> relationship, not a technical one. It grants no
/// dependency permission — every actual directed dependency still requires its own
/// <see cref="UpstreamAttribute"/> declaration on the downstream side. A typical example is a
/// consumer-defined trigger interface: the consumer owns the contract, the producer implements it,
/// and both must evolve it together.
/// </para>
/// <para>
/// <b>Usage:</b> place on the bounded context's marker class. Partnerships are symmetric — the
/// declaration must exist on <em>both</em> contexts:
/// </para>
/// <example>
/// <code>
/// namespace Acme.Shop.Cart;
///
/// [BoundedContext("Shopping Cart")]
/// [Partnership("Checkout", Rationale = "ICartCompletionTrigger contract evolves jointly")]
/// public static class CartContext
/// {
/// }
/// </code>
/// </example>
/// <para><b>Architectural rules</b> (enforced by the context-map rules):</para>
/// <list type="bullet">
///   <item><description>Only classes annotated with <see cref="Strategic.BoundedContextAttribute"/> may declare <c>[Partnership]</c></description></item>
///   <item><description>The target context must exist and must not be the declaring context itself</description></item>
///   <item><description>The declaration must be mirrored by the target context (symmetry)</description></item>
/// </list>
/// </remarks>
/// <seealso cref="UpstreamAttribute"/>
/// <seealso cref="Strategic.BoundedContextAttribute"/>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class PartnershipAttribute : Attribute
{
    /// <summary>
    /// Declares a partnership.
    /// </summary>
    /// <param name="context">Module name of the partner bounded context (e.g. <c>"Checkout"</c>).</param>
    public PartnershipAttribute(string context)
    {
        Context = context;
    }

    /// <summary>
    /// Module name of the partner bounded context (e.g. <c>"Checkout"</c>).
    /// </summary>
    public string Context { get; }

    /// <summary>
    /// What the two contexts co-evolve and why. Defaults to empty.
    /// </summary>
    public string Rationale { get; set; } = "";
}
