namespace DomainCentric.BuildingBlocks.Ddd.Tactical;

/// <summary>
/// Marker interface for Domain Gateways.
/// </summary>
/// <remarks>
/// <para>
/// A Domain Gateway is an interface declared in the <b>domain layer</b> that the domain itself uses
/// to consult external facts or delegate technology-bound operations, without coupling the domain
/// to framework or infrastructure types. The implementation lives outside the domain (typically in
/// an outgoing adapter), but the contract is owned by the domain and expressed in domain language.
/// </para>
/// <para><b>How it differs from related concepts:</b></para>
/// <list type="bullet">
///   <item><description><b>vs Domain Service</b> — an <see cref="IDomainService"/> contains pure domain logic;
///   both interface and implementation live in the domain layer (framework-free). A Domain Gateway is a
///   <i>port</i> to the outside world; only the interface is in the domain.</description></item>
///   <item><description><b>vs Output Port</b> — an Output Port lives in the application layer and is used by
///   use cases. A Domain Gateway lives in the domain layer and is used by aggregates, entities, or domain
///   services to enforce invariants or perform domain-bound operations.</description></item>
///   <item><description><b>vs Repository</b> — a Repository persists and reconstitutes aggregates. A Domain
///   Gateway exposes external capability (cryptography, availability check, geocoding, …).</description></item>
/// </list>
/// <para><b>Characteristics:</b></para>
/// <list type="bullet">
///   <item><description>Interface in the domain layer (<c>{Context}.Domain.Gateway</c>)</description></item>
///   <item><description>No framework dependencies in the interface</description></item>
///   <item><description>Implementation in the outgoing adapter layer</description></item>
///   <item><description>Typically called by aggregates, entities, or domain services</description></item>
///   <item><description>Side-effect-free or read-only operations are the typical case</description></item>
/// </list>
/// <para><b>Example use cases:</b></para>
/// <list type="bullet">
///   <item><description>Password hashing/verification (<c>IPasswordHasher</c>)</description></item>
///   <item><description>Shipping availability checks during <c>Order.Confirm()</c></description></item>
///   <item><description>Tax rate lookup when the aggregate computes totals</description></item>
/// </list>
/// <para>
/// <b>Reference:</b> the pattern follows Vaughn Vernon's Implementing Domain-Driven Design (2013)
/// sample code (the identity-and-access <c>User</c> aggregate using an <c>EncryptionService</c> via
/// a domain registry), generalized as a typed marker rather than a service locator.
/// </para>
/// </remarks>
public interface IDomainGateway
{
}
