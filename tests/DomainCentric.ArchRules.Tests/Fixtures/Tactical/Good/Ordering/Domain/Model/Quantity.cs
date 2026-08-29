using System;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Domain.Model;

/// <summary>A hand-written (non-record) value object: sealed class, readonly fields, attribute equality.</summary>
public sealed class Quantity : IValue
{
    private static int instances;
    private readonly int _amount;

    public Quantity(int amount)
    {
        if (amount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Quantity must be positive");
        }

        _amount = amount;
        instances++;
    }

    public int Amount => _amount;

    public static int Instances => instances;

    public Quantity Plus(Quantity other) => new(_amount + other._amount);

    public override bool Equals(object? obj) => obj is Quantity other && other._amount == _amount;

    public override int GetHashCode() => _amount.GetHashCode();
}
