using System;
using System.Collections.Generic;

namespace DomainCentric.ArchRules.Tests.Fixtures.Primitives.Bad.Ledger.Domain.Model;

/// <summary>
/// An aggregate holding a count, a moment and a list of strings bare, and taking flag arguments - a bool parameter, a public
/// bool field and a bool property with a public setter. Its yes/no fact Closed, a bool property with a private setter, passes.
/// </summary>
public sealed class Account
{
    public bool Dormant;

    private int _overdraftCount;

    public Account(AccountId id) => Id = id;

    public AccountId Id { get; }

    public bool Closed { get; private set; }

    public bool Frozen { get; set; }

    public DateTimeOffset OpenedAt { get; private set; }

    public IReadOnlyList<string> Tags { get; } = new List<string>();

    public Guid OwnerId { get; private set; }

    public void Rename(string holder)
    {
    }

    public void MarkClosed(bool closed) => Closed = closed;

    public bool IsOverdrawn() => _overdraftCount > 0;

    public override string ToString() => Id.Value;
}
