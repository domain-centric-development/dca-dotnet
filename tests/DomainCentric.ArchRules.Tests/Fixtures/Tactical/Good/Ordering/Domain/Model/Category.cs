using System.Collections.Generic;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Domain.Model;

public readonly record struct CategoryId(string Value) : IId, IValue;

/// <summary>
/// A self-reference of the own type is tolerated by DCA-TAC-003; other instances of the same aggregate are
/// referenced by their ids.
/// </summary>
public sealed class Category : AggregateRootBase<Category, CategoryId>
{
    public Category(CategoryId id, Category? parent)
    {
        Id = id;
        Parent = parent;
    }

    public override CategoryId Id { get; }

    public Category? Parent { get; }

    public IReadOnlyList<CategoryId> ChildIds { get; } = new List<CategoryId>();
}
