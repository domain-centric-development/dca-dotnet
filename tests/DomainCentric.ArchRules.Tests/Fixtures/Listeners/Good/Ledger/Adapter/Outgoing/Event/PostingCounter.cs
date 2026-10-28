using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Listeners.Good.Infrastructure.Events;
using DomainCentric.ArchRules.Tests.Fixtures.Listeners.Good.Ledger.Application.Shared;

namespace DomainCentric.ArchRules.Tests.Fixtures.Listeners.Good.Ledger.Adapter.Outgoing.Event;

/// <summary>A synchronous listener that calls an output port only.</summary>
public sealed class PostingCounter : IEventListener
{
    private readonly IPostingMetrics _metrics;

    public PostingCounter(IPostingMetrics metrics) => _metrics = metrics;

    public Task OnAsync(object @event, CancellationToken cancellationToken = default)
    {
        _metrics.Count(@event.ToString() ?? "");
        return Task.CompletedTask;
    }
}
