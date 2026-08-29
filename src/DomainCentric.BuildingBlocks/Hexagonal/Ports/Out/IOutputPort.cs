namespace DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

/// <summary>
/// Marker interface for Output Ports (Hexagonal Architecture).
/// </summary>
/// <remarks>
/// <para>
/// Output ports define what the application needs from the outside world. They represent
/// dependencies that the application layer requires but does not implement itself. In Hexagonal
/// Architecture terms, output ports sit on the "right side" of the hexagon.
/// </para>
/// <para><b>Driven adapters that implement Output Ports:</b></para>
/// <list type="bullet">
///   <item><description>Repository implementations (database access)</description></item>
///   <item><description>External API clients (REST, gRPC, SOAP)</description></item>
///   <item><description>Message publishers (Kafka, RabbitMQ, SQS)</description></item>
///   <item><description>Email/SMS services</description></item>
///   <item><description>File storage services</description></item>
///   <item><description>Cache implementations</description></item>
/// </list>
/// <para><b>Common Output Port types:</b></para>
/// <list type="bullet">
///   <item><description><see cref="IRepository{TAggregate, TId}"/> — aggregate persistence</description></item>
///   <item><description><see cref="IDomainEventPublisher"/> — event publication</description></item>
///   <item><description>External service ports — integration with external systems</description></item>
/// </list>
/// <para><b>Key characteristics:</b></para>
/// <list type="bullet">
///   <item><description>Technology-agnostic interface (no framework dependencies)</description></item>
///   <item><description>Defined in terms of application/domain concepts</description></item>
///   <item><description>Implemented by adapters in the outgoing adapter layer</description></item>
///   <item><description>Used (depended upon) by application layer classes</description></item>
/// </list>
/// <para><b>Example hierarchy:</b></para>
/// <code>
/// IOutputPort (marker)
///   ├── IRepository&lt;TAggregate, TId&gt;
///   │     └── IProductRepository : IRepository&lt;Product, ProductId&gt;
///   └── IDomainEventPublisher
/// </code>
/// <para>
/// <b>Dependency Inversion Principle:</b> output ports enable the application layer to depend on
/// abstractions rather than concrete implementations. The application defines what it needs (the
/// interface), and the infrastructure provides concrete implementations.
/// </para>
/// </remarks>
/// <seealso cref="IRepository{TAggregate, TId}"/>
/// <seealso cref="IDomainEventPublisher"/>
/// <seealso cref="In.IInputPort"/>
public interface IOutputPort
{
}
