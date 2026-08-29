namespace DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

/// <summary>
/// Marker interface for Stores — output ports that record or query operational data without an own
/// aggregate lifecycle.
/// </summary>
/// <remarks>
/// <para><b>Repository vs. Store:</b></para>
/// <list type="bullet">
///   <item><description>Use <see cref="IRepository{TAggregate, TId}"/> for <b>Aggregate Roots</b> with identity and
///   lifecycle (find by id, save, delete).</description></item>
///   <item><description>Use <see cref="IStore"/> for <b>Value Objects, Events, or operational data</b> without
///   identity-based access (record, count, exists).</description></item>
/// </list>
/// <para><b>Examples of Stores:</b></para>
/// <list type="bullet">
///   <item><description><c>ILoginProtectionStore</c> — records login attempts; queries failure counts</description></item>
///   <item><description><c>IAuditLogStore</c> — appends audit entries; queries by time range</description></item>
///   <item><description><c>IEventStore</c> (Event Sourcing) — specialization for Domain Events</description></item>
/// </list>
/// <para><b>Rules of thumb:</b></para>
/// <list type="number">
///   <item><description>Need <c>FindByIdAsync()</c>? → <see cref="IRepository{TAggregate, TId}"/> (object has identity)</description></item>
///   <item><description>Need <c>RecordAsync()</c> or <c>CountAsync()</c>? → <see cref="IStore"/> (object is recorded, not managed)</description></item>
///   <item><description>In doubt: if the stored object is an <c>IValue</c> or a record, it's almost always a Store.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="IRepository{TAggregate, TId}"/>
/// <seealso cref="IOutputPort"/>
public interface IStore : IOutputPort
{
}
