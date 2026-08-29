using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad.SharedKernel.Domain.Model;

/// <summary>
/// DCA-TAC-009 (not sealed), DCA-TAC-010 (non-readonly field), DCA-TAC-011 (setter), DCA-TAC-012 (no
/// Equals/GetHashCode).
/// </summary>
public class Money : IValue
{
    private decimal _amount;
    private readonly string _currency;

    public Money(decimal amount, string currency)
    {
        _amount = amount;
        _currency = currency;
    }

    public decimal Amount => _amount;

    public string Currency => _currency;

    public void SetAmount(decimal amount) => _amount = amount;
}
