using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace Acme.Shop.Cart.Domain.Model;

public readonly record struct Quantity : IValue
{
    public Quantity(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "quantity must be positive");
        }

        Amount = amount;
    }

    public int Amount { get; }
}
