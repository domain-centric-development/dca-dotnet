using System;
using System.Threading;
using System.Threading.Tasks;

namespace DomainCentric.BuildingBlocks.Application.Transactions;

/// <summary>
/// Explicit transaction boundary inside a use case — an application-layer execution abstraction,
/// <b>not</b> an output port.
/// </summary>
/// <remarks>
/// <para>An output port describes a capability the application needs from the outside world (store an aggregate,
/// look up a price, publish an event). A transaction is no such interaction: it defines the execution semantics
/// of several of them. The interface therefore lives beside the use cases and does not derive from
/// <c>IOutputPort</c>; the implementation is infrastructure (EF Core: <c>DbContext</c> transaction +
/// <c>SaveChangesAsync</c>; ADO.NET: <c>TransactionScope</c>).</para>
/// <para>The default boundary is a decorator or pipeline around <see cref="Hexagonal.Ports.In.IUseCase{TInput, TOutput}"/>:
/// load, mutate, save, publish — all inside one short transaction. That default breaks down as soon as the use
/// case also talks to the outside world (payment provider, remote catalog, mail gateway): a remote call inside the
/// transaction holds a database connection for the duration of the call, and under load the connection pool runs
/// dry; a rollback after a successful remote call cannot undo the remote effect either.</para>
/// <para><see cref="ITransactionBoundary"/> lets the use case draw the boundary by hand — remote reads before, the
/// transactional core inside, remote effects after (preferably as a reaction to an integration event):</para>
/// <code>
/// var article = await _articles.GetArticleDataAsync(productId, ct);        // remote read, no transaction
/// return await _transactionBoundary.InTransactionAsync(async c =>          // short transaction
/// {
///     var cart = await _carts.FindByIdAsync(cartId, c) ?? throw new ArgumentException(...);
///     cart.AddItem(productId, quantity, article.Price);
///     await _carts.SaveAsync(cart, c);
///     await _events.PublishAndClearEventsAsync(cart, c);
///     return AddItemToCartResult.From(cart);
/// }, ct);
/// </code>
/// <para>Domain events published inside <see cref="InTransactionAsync{T}"/> see the same transaction as the save;
/// integration events registered inside it become visible to their dispatcher when it commits.</para>
/// <para><b>Nesting.</b> A call inside a running transaction joins it — there is one commit, at the outermost
/// boundary. A failure in an inner block marks the shared transaction rollback-only even when the outer block
/// catches the exception: the outermost <c>InTransactionAsync</c> then rolls back and throws instead of committing
/// half of the work. Implementations must preserve this; an in-memory implementation emulates it with a
/// rollback-only flag.</para>
/// <para><b>Rules of thumb:</b></para>
/// <list type="number">
///   <item><description>No remote call inside a transaction — neither in a decorated use case nor inside
///   <c>InTransactionAsync</c>.</description></item>
///   <item><description>One aggregate per transaction; cross-aggregate consistency is eventual.</description></item>
///   <item><description>Use the decorator when the whole use case is local; use <see cref="ITransactionBoundary"/>
///   when it is not.</description></item>
/// </list>
/// </remarks>
public interface ITransactionBoundary
{
    /// <summary>
    /// Runs <paramref name="work"/> inside one transaction and returns its result; the transaction commits when
    /// the work completes and rolls back when it throws.
    /// </summary>
    Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default);

    /// <summary>Runs <paramref name="work"/> inside one transaction; convenience for work without a result.</summary>
    Task InTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default) =>
        InTransactionAsync(
            async ct =>
            {
                await work(ct).ConfigureAwait(false);
                return true;
            },
            cancellationToken);
}
