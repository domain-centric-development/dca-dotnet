using System;
using System.Collections.Generic;

namespace DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

/// <summary>
/// Declares that this bounded context consumes another bounded context as its upstream.
/// </summary>
/// <remarks>
/// <para>
/// Placed on the <em>downstream</em> side because that is where the dependency originates and where
/// the translation decision is made. The upstream's publication style is not declared here — it is
/// expressed by the upstream itself through its <c>Api</c>/<c>Events</c> published interfaces and
/// <see cref="OpenHostServiceAttribute"/>.
/// </para>
/// <para>
/// <c>[Upstream]</c> declares only the directed dependency and how the downstream protects its
/// model. Organizational patterns such as Customer–Supplier are not machine-classified; document
/// them in <see cref="Rationale"/> if relevant.
/// </para>
/// <para>
/// <b>Usage:</b> place on the bounded context's marker class (the class carrying
/// <see cref="Strategic.BoundedContextAttribute"/>), one attribute per <c>(context, translation)</c>
/// pair. Different translation strategies per channel require separate attributes:
/// </para>
/// <example>
/// <code>
/// namespace Acme.Shop.Cart;
///
/// [BoundedContext("Shopping Cart")]
/// [Upstream("Product", Translation.AntiCorruptionLayer, Consumes.Api)]
/// [Upstream("Product", Translation.Conformist, Consumes.Events)]
/// public static class CartContext
/// {
/// }
/// </code>
/// </example>
/// <para><b>Architectural rules</b> (enforced by the context-map rules):</para>
/// <list type="bullet">
///   <item><description>Only classes annotated with <see cref="Strategic.BoundedContextAttribute"/> may declare <c>[Upstream]</c></description></item>
///   <item><description>The target context must exist and must not be the declaring context itself</description></item>
///   <item><description><c>(context, via)</c> must be unique across all declarations of one context</description></item>
///   <item><description>Every <see cref="UpstreamStatus.Implemented"/> declaration must be backed by at least one actual
///   code dependency on the declared channel namespace — a declaration without code is only legal as
///   <see cref="UpstreamStatus.Planned"/></description></item>
///   <item><description><see cref="Translation.AntiCorruptionLayer"/> + <see cref="Consumes.Api"/>: upstream contract
///   types appear only in the downstream's outgoing adapters</description></item>
///   <item><description><see cref="Translation.AntiCorruptionLayer"/> + <see cref="Consumes.Events"/>: upstream contract
///   types appear only in the downstream's incoming adapters</description></item>
///   <item><description><see cref="Translation.Conformist"/>: upstream contract types may appear outside adapters but
///   never in the downstream's domain layer</description></item>
/// </list>
/// </remarks>
/// <seealso cref="PartnershipAttribute"/>
/// <seealso cref="OpenHostServiceAttribute"/>
/// <seealso cref="Strategic.BoundedContextAttribute"/>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class UpstreamAttribute : Attribute
{
    /// <summary>
    /// Declares an upstream relationship.
    /// </summary>
    /// <param name="context">Module name of the upstream bounded context (e.g., <c>"Product"</c>).</param>
    /// <param name="translation">How this context protects its model from the upstream's model.</param>
    /// <param name="via">Which of the upstream's published interfaces this context consumes. Must not be empty.</param>
    public UpstreamAttribute(string context, Translation translation, params Consumes[] via)
    {
        Context = context;
        Translation = translation;
        Via = via ?? Array.Empty<Consumes>();
    }

    /// <summary>
    /// Module name of the upstream bounded context (e.g., <c>"Product"</c>).
    /// </summary>
    public string Context { get; }

    /// <summary>
    /// How this context protects its model from the upstream's model.
    /// </summary>
    public Translation Translation { get; }

    /// <summary>
    /// Which of the upstream's published interfaces this context consumes. Must not be empty.
    /// </summary>
    public IReadOnlyList<Consumes> Via { get; }

    /// <summary>
    /// Why this relationship exists and why this translation was chosen. Defaults to empty.
    /// </summary>
    public string Rationale { get; set; } = "";

    /// <summary>
    /// Whether the relationship exists in code or is only intended.
    /// <see cref="UpstreamStatus.Implemented"/> declarations must be backed by an actual dependency
    /// on the declared channel namespace (enforced); <see cref="UpstreamStatus.Planned"/>
    /// declarations document intent, appear as such in the generated context map, and are exempt
    /// from the existence rule. Defaults to <see cref="UpstreamStatus.Implemented"/>.
    /// </summary>
    public UpstreamStatus Status { get; set; } = UpstreamStatus.Implemented;
}
