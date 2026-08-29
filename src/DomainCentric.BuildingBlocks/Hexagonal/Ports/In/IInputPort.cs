namespace DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

/// <summary>
/// Marker interface for Input Ports (Hexagonal Architecture).
/// </summary>
/// <remarks>
/// <para>
/// Input ports are the entry points to the application layer. They define how the outside world
/// (driving/primary adapters) can interact with the application. In Hexagonal Architecture terms,
/// input ports sit on the "left side" of the hexagon.
/// </para>
/// <para><b>Driving adapters that use Input Ports:</b></para>
/// <list type="bullet">
///   <item><description>REST Controllers</description></item>
///   <item><description>GraphQL Resolvers</description></item>
///   <item><description>CLI Command Handlers</description></item>
///   <item><description>Event Consumers (external events)</description></item>
///   <item><description>Scheduled Tasks</description></item>
///   <item><description>MCP Tool Providers</description></item>
/// </list>
/// <para><b>Common Input Port types:</b></para>
/// <list type="bullet">
///   <item><description><see cref="IUseCase{TInput, TOutput}"/> — Command/Query pattern with input and output types</description></item>
///   <item><description>Event Handlers — process incoming events from other systems</description></item>
/// </list>
/// <para><b>Key characteristics:</b></para>
/// <list type="bullet">
///   <item><description>Technology-agnostic (no framework dependencies)</description></item>
///   <item><description>Defined in terms of application/domain concepts</description></item>
///   <item><description>Implemented by application layer classes (use cases)</description></item>
///   <item><description>Called by adapters in the incoming adapter layer</description></item>
/// </list>
/// <para><b>Example hierarchy:</b></para>
/// <code>
/// IInputPort (marker)
///   └── IUseCase&lt;TInput, TOutput&gt;
///         └── ICreateProductInputPort : IUseCase&lt;CreateProductCommand, CreateProductResult&gt;
/// </code>
/// </remarks>
/// <seealso cref="IUseCase{TInput, TOutput}"/>
/// <seealso cref="Out.IOutputPort"/>
public interface IInputPort
{
}
