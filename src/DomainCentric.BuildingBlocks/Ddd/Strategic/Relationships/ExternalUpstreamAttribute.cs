using System;

namespace DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

/// <summary>
/// Declares that this bounded context consumes an <em>external system</em> — one that lives outside
/// this codebase — as its upstream.
/// </summary>
/// <remarks>
/// <para>
/// Like <see cref="UpstreamAttribute"/>, this is declared on the downstream side, because an
/// external system has no side in this codebase that could declare anything. The model dependency
/// always points to the external system, regardless of who initiates the exchange: a webhook the
/// external system calls is still <em>its</em> contract that this context conforms to or translates.
/// </para>
/// <para>
/// <see cref="Interaction"/> names who initiates — which is also where the edge sits and where an
/// Anti-Corruption Layer must live. Protocol details (webhook, queue, SFTP batch, polling cadence)
/// belong in <see cref="Rationale"/>; polling an external API is
/// <see cref="Relationships.Interaction.Outbound"/>, no matter how event-like its semantics.
/// </para>
/// <example>
/// <code>
/// namespace Acme.Shop.Checkout;
///
/// [BoundedContext("Checkout")]
/// [ExternalUpstream("Payment Service Provider", Translation.AntiCorruptionLayer, Interaction.Outbound,
///     Rationale = "Synchronous payment operations behind the caller-owned IPaymentProvider port")]
/// public static class CheckoutContext
/// {
/// }
/// </code>
/// </example>
/// <para><b>Architectural rules</b> (enforced by the context-map rules):</para>
/// <list type="bullet">
///   <item><description>Only classes annotated with <see cref="Strategic.BoundedContextAttribute"/> may declare <c>[ExternalUpstream]</c></description></item>
///   <item><description><see cref="Name"/> must not be blank and must not collide with an internal context's module name</description></item>
///   <item><description><c>(name, interaction)</c> must be unique per declaring context</description></item>
///   <item><description><see cref="Translation.AntiCorruptionLayer"/>: types from <see cref="ContractNamespaces"/> appear only
///   in the adapter matching the interaction (outgoing for <see cref="Relationships.Interaction.Outbound"/>, incoming for
///   <see cref="Relationships.Interaction.Inbound"/>)</description></item>
///   <item><description><see cref="Translation.Conformist"/>: types from <see cref="ContractNamespaces"/> never reach the domain layer</description></item>
/// </list>
/// <para>
/// Without <see cref="ContractNamespaces"/> (plain HTTP, no vendor SDK) the translation rules have
/// nothing to check — the declaration then documents the relationship and feeds the generated
/// context map.
/// </para>
/// </remarks>
/// <seealso cref="UpstreamAttribute"/>
/// <seealso cref="Strategic.BoundedContextAttribute"/>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ExternalUpstreamAttribute : Attribute
{
    /// <summary>
    /// Declares an external upstream relationship.
    /// </summary>
    /// <param name="name">Display name of the external system (e.g. <c>"Payment Service Provider"</c>).</param>
    /// <param name="translation">How this context protects its model from the external system's model.</param>
    /// <param name="interaction">Who initiates the exchange — and therefore on which adapter side the edge sits.</param>
    public ExternalUpstreamAttribute(string name, Translation translation, Interaction interaction)
    {
        Name = name;
        Translation = translation;
        Interaction = interaction;
    }

    /// <summary>
    /// Display name of the external system (e.g. <c>"Payment Service Provider"</c>).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// How this context protects its model from the external system's model.
    /// </summary>
    public Translation Translation { get; }

    /// <summary>
    /// Who initiates the exchange — and therefore on which adapter side the edge sits.
    /// </summary>
    public Interaction Interaction { get; }

    /// <summary>
    /// Namespaces of the external system's contract types (vendor SDK, generated client), as
    /// namespace prefixes (e.g. <c>"Stripe"</c>). Empty when the contract is wire-level only.
    /// </summary>
    public string[] ContractNamespaces { get; set; } = Array.Empty<string>();

    /// <summary>
    /// The exchange mechanism as a single word — e.g. <c>"webhook"</c>, <c>"REST"</c>,
    /// <c>"queue"</c>, <c>"SFTP"</c>. Shown in the context map diagram's edge label, where
    /// <c>inbound</c>/<c>outbound</c> alone says little about the kind of interaction.
    /// </summary>
    public string Protocol { get; set; } = "";

    /// <summary>
    /// What flows over this edge, as a one-line naming of operations, event types, or payload —
    /// e.g. <c>"payment operations (initiate, confirm, refund)"</c>. Rendered in the generated
    /// context map's tables (the diagram carries only <see cref="Protocol"/> to keep edge labels
    /// short). Unlike an internal upstream, an external system has no code in this codebase that
    /// could say this. Keep it a naming, not a schema — a vendor SDK belongs in
    /// <see cref="ContractNamespaces"/>, a full contract in a schema artifact.
    /// </summary>
    public string Exchanges { get; set; } = "";

    /// <summary>
    /// Why this relationship exists, which protocol it uses, and why this translation was chosen.
    /// </summary>
    public string Rationale { get; set; } = "";

    /// <summary>
    /// Whether the integration exists in code or is only intended. An external system's wire-level
    /// contract leaves no checkable edge, so <see cref="UpstreamStatus.Implemented"/> is not
    /// machine-verified here — but <see cref="UpstreamStatus.Planned"/> keeps the generated context
    /// map honest: the relationship is rendered as planned instead of posing as an existing integration.
    /// </summary>
    public UpstreamStatus Status { get; set; } = UpstreamStatus.Implemented;
}
