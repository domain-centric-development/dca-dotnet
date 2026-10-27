using System.Threading;
using System.Threading.Tasks;

namespace DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

/// <summary>
/// The most common kind of <see cref="IInputPort"/>: one operation that takes a command or query and
/// answers with a result — a Use Case in Clean Architecture terms.
/// </summary>
/// <remarks>
/// <para>
/// Every use case is an input port; not every input port is a use case. An input port that only
/// receives an incoming event or serves operations without a result model extends
/// <see cref="IInputPort"/> directly. A use case is called by primary/driving adapters (e.g., REST
/// controllers, event consumers, CLI handlers).
/// </para>
/// <para><b>Characteristics of a Use Case:</b></para>
/// <list type="bullet">
///   <item><description>Represents a single user action or system operation</description></item>
///   <item><description>Is the interface the application-layer implementation implements</description></item>
///   <item><description>Accepts an input model (Command or Query) as its parameter</description></item>
///   <item><description>Returns an output model, not a domain entity</description></item>
///   <item><description>Technology-agnostic (no framework dependencies)</description></item>
/// </list>
/// <para>
/// <b>Input/Output pattern:</b> a use case accepts an input model and returns an output model to
/// decouple the application layer from presentation and infrastructure concerns.
/// </para>
/// <para>
/// <b>Asynchronous by design:</b> a use case orchestrates output ports (persistence, messaging,
/// remote systems), which are inherently I/O-bound in .NET, so the port is
/// <see cref="Task{TResult}"/>-based and accepts a <see cref="CancellationToken"/>. There is
/// deliberately no synchronous twin. The <i>domain layer</i>, in contrast, stays synchronous:
/// aggregates, entities, value objects and domain services contain pure logic and never await.
/// </para>
/// <example>
/// <code>
/// public interface ICreateProductInputPort : IUseCase&lt;CreateProductCommand, CreateProductResult&gt;
/// {
/// }
///
/// public sealed class CreateProductUseCase : ICreateProductInputPort
/// {
///     private readonly IProductRepository _products;
///
///     public CreateProductUseCase(IProductRepository products) => _products = products;
///
///     public async Task&lt;CreateProductResult&gt; ExecuteAsync(
///         CreateProductCommand input, CancellationToken cancellationToken = default)
///     {
///         var product = Product.Create(input.Name, input.Sku, input.Price);
///         await _products.SaveAsync(product, cancellationToken);
///         return new CreateProductResult(product.Id);
///     }
/// }
/// </code>
/// </example>
/// <para>
/// <b>Naming convention:</b> the port is named <c>I{Action}{Entity}InputPort</c>, the implementation
/// <c>{Action}{Entity}UseCase</c> (e.g., <c>ICreateProductInputPort</c> / <c>CreateProductUseCase</c>).
/// </para>
/// <para><b>References:</b></para>
/// <list type="bullet">
///   <item><description>Alistair Cockburn — Hexagonal Architecture (Ports &amp; Adapters)</description></item>
///   <item><description>Robert C. Martin — Clean Architecture (Chapters 19–20: Use Cases)</description></item>
///   <item><description>Tom Hombergs — Get Your Hands Dirty on Clean Architecture</description></item>
/// </list>
/// </remarks>
/// <typeparam name="TInput">The input model type (Command or Query).</typeparam>
/// <typeparam name="TOutput">The output model type (Result).</typeparam>
public interface IUseCase<in TInput, TOutput> : IInputPort
{
    /// <summary>
    /// Executes this use case with the given input.
    /// </summary>
    /// <param name="input">The use case input (Command or Query).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task producing the use case output (Result).</returns>
    Task<TOutput> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);
}
