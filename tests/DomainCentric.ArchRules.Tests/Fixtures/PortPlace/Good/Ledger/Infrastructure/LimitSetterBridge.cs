using DomainCentric.ArchRules.Tests.Fixtures.PortPlace.Good.Ledger.Application.ChangeLimit;
using DomainCentric.ArchRules.Tests.Fixtures.PortPlace.Good.Ledger.Application.Shared;

namespace DomainCentric.ArchRules.Tests.Fixtures.PortPlace.Good.Ledger.Infrastructure;

/// <summary>Hands the output port a second use case: wiring, not an adapter.</summary>
public sealed class LimitSetterBridge : ILimitSetter
{
    private readonly IChangeLimitInputPort _changeLimit;

    public LimitSetterBridge(IChangeLimitInputPort changeLimit) => _changeLimit = changeLimit;

    public void SetLimit(string account, long amount) => _changeLimit.Change(account, amount);
}
