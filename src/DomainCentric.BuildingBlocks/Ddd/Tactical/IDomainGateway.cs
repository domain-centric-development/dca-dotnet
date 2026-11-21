namespace DomainCentric.BuildingBlocks.Ddd.Tactical;

/// <summary>
/// Marker interface for Domain Gateways.
/// </summary>
/// <remarks>
/// <para>
/// A Domain Gateway is an interface declared in the <b>domain layer</b> through which the domain
/// obtains facts it does not hold, or delegates a technology-bound computation, without coupling the
/// domain to framework or infrastructure types. The implementation lives outside the domain
/// (typically in an outgoing adapter), but the contract is owned by the domain and expressed in
/// domain language.
/// </para>
/// <para>
/// <b>Who calls it.</b> A domain service, the application (a use case), or a model method that
/// receives the gateway as a parameter. The aggregate, entity, value object, factory or
/// specification that needs it takes it in the signature of the method that needs it, and the
/// domain service or the use case passes it in. It never holds a gateway in a field or property,
/// never looks one up from a registry and never builds an implementation itself: the dependency
/// stays visible in the signature, and the model keeps no reference to the outside between two calls.
/// </para>
/// <para>
/// <b>It never writes to the outside.</b> A gateway enriches the model with information so that the
/// model can decide; it does not change the state of any external system. Persisting, sending,
/// publishing and calling a remote operation that has an effect are the application's business,
/// through an output port. A computation that touches nothing outside the process — hashing a
/// password, converting a currency with a supplied rate — is a gateway, because no external state
/// changes.
/// </para>
/// <para><b>How it differs from related concepts:</b></para>
/// <list type="bullet">
///   <item><description><b>vs Domain Service</b> — an <see cref="IDomainService"/> contains pure domain logic;
///   both interface and implementation live in the domain layer (framework-free). A Domain Gateway is a
///   <i>port</i> to the outside world; only the interface is in the domain.</description></item>
///   <item><description><b>vs Output Port</b> — an Output Port lives in the application layer and is used by
///   use cases. A Domain Gateway lives in the domain layer and is used by domain services, by use cases and
///   by the model method it is passed to, to obtain the facts an invariant needs.</description></item>
///   <item><description><b>vs Repository</b> — a Repository persists and reconstitutes aggregates. A Domain
///   Gateway exposes external capability (cryptography, availability check, geocoding, …).</description></item>
/// </list>
/// <para><b>Characteristics:</b></para>
/// <list type="bullet">
///   <item><description>Interface in the domain layer (<c>{Context}.Domain.Gateway</c>)</description></item>
///   <item><description>No framework dependencies in the interface</description></item>
///   <item><description>Implementation in the outgoing adapter layer</description></item>
///   <item><description>Called by a domain service, by the application, or by a model method that receives
///   it as a parameter; never held in a field or property or looked up by the model</description></item>
///   <item><description>Read-only towards the outside: no external state is created, changed or removed</description></item>
/// </list>
/// <para><b>Example use cases:</b></para>
/// <list type="bullet">
///   <item><description>Password hashing: the use case passes an <c>IPasswordHasher</c> to
///   <c>Account.Register(..., hasher)</c>, which hashes the password through it</description></item>
///   <item><description>Availability check: a domain service asks the gateway and passes the answer to
///   <c>Order.Confirm(...)</c></description></item>
///   <item><description>Tax rate lookup: the use case looks up the rate and hands it to the aggregate that
///   computes the totals</description></item>
/// </list>
/// <para>
/// <b>Reference:</b> the pattern starts from Vaughn Vernon's Implementing Domain-Driven Design (2013)
/// sample code (the identity-and-access <c>EncryptionService</c> reached via a domain registry),
/// generalized as a typed marker rather than a service locator. Where the sample's <c>User</c> aggregate
/// asks the domain registry for the service, DCA passes the gateway in as a parameter: no registry, no
/// hidden dependency.
/// </para>
/// </remarks>
public interface IDomainGateway
{
}
