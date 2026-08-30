using System;
using System.Threading;
using System.Threading.Tasks;

namespace DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

/// <summary>
/// Output port for an explicit transaction boundary inside a use case.
/// </summary>
/// <remarks>
/// <para>The default transaction boundary is the use case itself (a decorator or pipeline around
/// <see cref="In.IUseCase{TInput, TOutput}"/>): load, mutate, save, publish — all inside one short transaction.
/// That default breaks down as soon as the use case also talks to the outside world (payment provider, remote
/// catalog, mail gateway): a remote call inside the transaction holds a database connection for the duration of
/// the call, and under load the connection pool runs dry; a rollback after a successful remote call cannot undo
/// the remote effect either.</para>
/// <para><see cref="IUnitOfWork"/> lets the use case draw the boundary by hand — remote reads before, the
/// transactional core inside, remote effects after (preferably as a reaction to an integration event):</para>
/// <code>
/// var article = await _articles.GetArticleDataAsync(productId, ct);   // remote read, no transaction
/// return await _unitOfWork.RunAsync(async c =>                        // short transaction
/// {
///     var cart = await _carts.FindByIdAsync(cartId, c) ?? throw new ArgumentException(...);
///     cart.AddItem(productId, quantity, article.Price);
///     await _carts.SaveAsync(cart, c);
///     await _events.PublishAndClearEventsAsync(cart, c);
///     return AddItemToCartResult.From(cart);
/// }, ct);
/// </code>
/// <para>The adapter binds the port to the platform's transaction (EF Core: <c>DbContext</c> transaction +
/// <c>SaveChangesAsync</c>; ADO.NET: <c>TransactionScope</c>). Domain events published inside
/// <see cref="RunAsync{T}"/> see the same transaction as the save; integration events registered inside it become
/// visible to their dispatcher when it commits.</para>
/// <para><b>Rules of thumb:</b></para>
/// <list type="number">
///   <item><description>No remote call inside a transaction — neither in a decorated use case nor inside
///   <c>RunAsync</c>.</description></item>
///   <item><description>One aggregate per transaction; cross-aggregate consistency is eventual.</description></item>
///   <item><description>Use the decorator when the whole use case is local; use <see cref="IUnitOfWork"/> when it
///   is not.</description></item>
/// </list>
/// </remarks>
/// <seealso cref="IDomainEventPublisher"/>
/// <seealso cref="IRepository{TAggregate, TId}"/>
public interface IUnitOfWork : IOutputPort
{
    /// <summary>
    /// Runs <paramref name="work"/> inside one transaction and returns its result; the transaction commits when
    /// the work completes and rolls back when it throws.
    /// </summary>
    Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default);

    /// <summary>Runs <paramref name="work"/> inside one transaction; convenience for work without a result.</summary>
    Task RunAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default) =>
        RunAsync(
            async ct =>
            {
                await work(ct).ConfigureAwait(false);
                return true;
            },
            cancellationToken);
}
