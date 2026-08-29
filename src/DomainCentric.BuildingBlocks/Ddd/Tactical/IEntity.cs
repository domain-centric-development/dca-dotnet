namespace DomainCentric.BuildingBlocks.Ddd.Tactical;

/// <summary>
/// Non-generic base marker for Entities.
/// </summary>
/// <remarks>
/// <para>
/// This interface has no members. It exists so that architecture rules and reflection-based
/// tooling can test whether a type is an entity (<c>typeof(IEntity).IsAssignableFrom(type)</c>)
/// without having to close the generic <see cref="IEntity{TSelf, TId}"/> over concrete type
/// arguments it cannot know in advance.
/// </para>
/// <para>
/// Domain code should implement <see cref="IEntity{TSelf, TId}"/>, never this interface directly.
/// </para>
/// </remarks>
public interface IEntity
{
}
