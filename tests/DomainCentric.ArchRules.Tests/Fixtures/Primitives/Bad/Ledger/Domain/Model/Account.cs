using System;
using System.Collections.Generic;

namespace DomainCentric.ArchRules.Tests.Fixtures.Primitives.Bad.Ledger.Domain.Model;

/// <summary>An aggregate holding a flag, a count, a moment and a list of strings bare.</summary>
public sealed class Account
{
    private int _overdraftCount;

    public Account(AccountId id) => Id = id;

    public AccountId Id { get; }

    public bool Closed { get; private set; }

    public DateTimeOffset OpenedAt { get; private set; }

    public IReadOnlyList<string> Tags { get; } = new List<string>();

    public void Rename(string holder)
    {
    }

    public bool IsOverdrawn() => _overdraftCount > 0;

    public override string ToString() => Id.Value;
}
