namespace DomainCentric.BuildingBlocks.Ddd.Tactical;

/// <summary>
/// Marker interface for an aggregate root that belongs to someone.
/// </summary>
/// <remarks>
/// <para>
/// An owned aggregate is reachable only through its owner. Its repository therefore declares a lookup that takes
/// the aggregate's id <em>and</em> the owner's id, and a use case acting for a caller loads it through that
/// lookup — never by id alone. An object loaded by its id alone is reachable by anyone who knows or guesses the
/// id: the insecure direct object reference of broken access control (OWASP Top 10, A01).
/// </para>
/// <para>
/// The type argument names the owner's id type: <c>MedicalRecord : IOwned&lt;PatientId&gt;</c>,
/// <c>Workspace : IOwned&lt;TenantId&gt;</c>. The marker declares no member; the owner is a property of the
/// aggregate like any other, and the owner id type is what the repository's scoped lookup takes.
/// </para>
/// <para><b>What it says, and what not:</b></para>
/// <list type="bullet">
/// <item>"Has an owner", not "always the same owner". The owner may change, but only through an explicit
/// operation of the aggregate — a draft a visitor started as a guest, taken over by the account they sign into —
/// called after the use case proved the caller's right to the change. That proof is logic of the use case, which
/// no marker can verify; its tests prove it.</item>
/// <item>An owner need not be an account. A guest given a pseudonymous identity on the first request is an owner:
/// the aggregate belongs to that identity, provided it comes from a signed token and is not guessable, never from
/// a value the client names.</item>
/// <item>Where no identity exists and the aggregate's id itself is the secret — a long random id in a link,
/// "whoever holds it may" — there is no owner to scope a lookup by, and the aggregate is not owned. Its safety is
/// the id's unguessability.</item>
/// </list>
/// <example>
/// <code>
/// public sealed class MedicalRecord : AggregateRootBase&lt;MedicalRecord, MedicalRecordId&gt;, IOwned&lt;PatientId&gt;
/// {
///     public PatientId Patient { get; private set; }
/// }
///
/// public interface IMedicalRecordRepository : IRepository&lt;MedicalRecord, MedicalRecordId&gt;
/// {
///     Task&lt;MedicalRecord?&gt; FindByIdForPatientAsync(MedicalRecordId id, PatientId patient, CancellationToken cancellationToken = default);
/// }
/// </code>
/// </example>
/// <para>Forgetting <c>IOwned&lt;…&gt;</c> silences every rule that selects on it; marking the aggregates that belong
/// to someone is part of modelling them.</para>
/// </remarks>
/// <typeparam name="TOwnerId">The id type of the owner.</typeparam>
public interface IOwned<TOwnerId> : IOwned
    where TOwnerId : IId
{
}
