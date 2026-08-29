namespace DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

/// <summary>
/// Non-generic base marker for Repositories.
/// </summary>
/// <remarks>
/// <para>
/// This interface has no members. It exists so that architecture rules and reflection-based
/// tooling can test whether a type is a repository without having to close the generic
/// <see cref="IRepository{TAggregate, TId}"/> over concrete type arguments it cannot know in advance.
/// </para>
/// <para>
/// Application code should declare repositories via <see cref="IRepository{TAggregate, TId}"/>,
/// never via this interface directly.
/// </para>
/// </remarks>
public interface IRepository : IOutputPort
{
}
