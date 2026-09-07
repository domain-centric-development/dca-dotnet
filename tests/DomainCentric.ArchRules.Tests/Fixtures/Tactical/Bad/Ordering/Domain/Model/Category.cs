using System.Collections.Generic;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.Ordering.Domain.Model;

public readonly record struct CategoryId(string Value) : IId, IValue;

/// <summary>
/// DCA-TAC-003: containers of the aggregate's own type hold other aggregate instances - a list, an array, a
/// dictionary and a nested list are all reported. The direct member <c>Root</c> of the own type is the one tolerated
/// shape (a self-reference is not another aggregate).
/// </summary>
public sealed class Category : AggregateRootBase<Category, CategoryId>
{
    public Category(CategoryId id, Category root)
    {
        Id = id;
        Root = root;
    }

    public override CategoryId Id { get; }

    public Category Root { get; }

    public IReadOnlyList<Category> Children { get; } = new List<Category>();

    public Category[] Siblings { get; } = new Category[0];

    public IReadOnlyDictionary<string, Category> ByName { get; } = new Dictionary<string, Category>();

    public IReadOnlyList<IReadOnlyList<Category>> Tree { get; } = new List<IReadOnlyList<Category>>();
}
