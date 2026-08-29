using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

/// <summary>
/// Base interface for Repositories.
/// </summary>
/// <remarks>
/// <para>
/// Repositories provide a collection-like interface for accessing Aggregate Roots. They encapsulate
/// the logic for retrieving and persisting aggregates, presenting the illusion of an in-memory
/// collection.
/// </para>
/// <para><b>Key principles:</b></para>
/// <list type="bullet">
///   <item><description>One Repository per Aggregate Root (not per Entity)</description></item>
///   <item><description>Repository interfaces are output ports owned by the application layer</description></item>
///   <item><description>Repository implementations belong in outgoing adapters</description></item>
///   <item><description>Use domain language in method names (not generic CRUD)</description></item>
///   <item><description>Return domain objects, never infrastructure objects</description></item>
/// </list>
/// <para><b>Characteristics:</b></para>
/// <list type="bullet">
///   <item><description>Interface resides in the application layer as an output port (e.g.,
///   <c>Product.Application.Shared.IProductRepository</c>)</description></item>
///   <item><description>Implementation resides in an outgoing adapter (e.g.,
///   <c>Product.Adapter.Outgoing.Persistence.InMemoryProductRepository</c>)</description></item>
///   <item><description>Methods use ubiquitous language (e.g., <c>FindBySkuAsync()</c>, <c>FindByCategoryAsync()</c>)</description></item>
///   <item><description>No framework attributes or dependencies in the interface</description></item>
///   <item><description>Collections should be read-only when returned</description></item>
/// </list>
/// <example>
/// <code>
/// // Application layer output port
/// public interface IProductRepository : IRepository&lt;Product, ProductId&gt;
/// {
///     Task&lt;Product?&gt; FindBySkuAsync(Sku sku, CancellationToken cancellationToken = default);
///     Task&lt;IReadOnlyList&lt;Product&gt;&gt; FindByCategoryAsync(Category category, CancellationToken cancellationToken = default);
///     Task&lt;bool&gt; ExistsBySkuAsync(Sku sku, CancellationToken cancellationToken = default);
/// }
///
/// // Outgoing adapter implementation
/// public sealed class InMemoryProductRepository : IProductRepository
/// {
///     // Implementation using in-memory storage
/// }
/// </code>
/// </example>
/// <para>
/// <b>Pattern:</b> repositories mediate between the domain and data mapping layers using a
/// collection-like interface for accessing domain objects.
/// </para>
/// <para><b>References:</b></para>
/// <list type="bullet">
///   <item><description>Eric Evans' Domain-Driven Design (2003), Chapter 6: "The Life Cycle of a Domain Object"</description></item>
///   <item><description>Vaughn Vernon's Implementing Domain-Driven Design (2013), Chapter 12: "Repositories"</description></item>
///   <item><description>Martin Fowler, Repository pattern (https://martinfowler.com/eaaCatalog/repository.html)</description></item>
/// </list>
/// </remarks>
/// <typeparam name="TAggregate">The aggregate root type.</typeparam>
/// <typeparam name="TId">The aggregate root identifier type.</typeparam>
public interface IRepository<TAggregate, TId> : IRepository
    where TAggregate : class, IAggregateRoot<TAggregate, TId>
    where TId : IId
{
    /// <summary>
    /// Finds an aggregate by its unique identifier.
    /// </summary>
    /// <param name="id">The aggregate identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The aggregate if found, otherwise <see langword="null"/>.</returns>
    Task<TAggregate?> FindByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an aggregate to the repository.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Adds a new aggregate or updates an existing one. The repository handles the distinction based
    /// on the aggregate's identity.
    /// </para>
    /// <para>After saving, domain events should be published by the application layer.</para>
    /// </remarks>
    /// <param name="aggregate">The aggregate to save.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The saved aggregate.</returns>
    Task<TAggregate> SaveAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an aggregate from the repository by its identifier.
    /// </summary>
    /// <remarks>
    /// Removes the aggregate from the collection. If the aggregate doesn't exist, the behavior is
    /// implementation-specific (may throw or silently succeed).
    /// </remarks>
    /// <param name="id">The identifier of the aggregate to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the aggregate has been deleted.</returns>
    Task DeleteByIdAsync(TId id, CancellationToken cancellationToken = default);
}
