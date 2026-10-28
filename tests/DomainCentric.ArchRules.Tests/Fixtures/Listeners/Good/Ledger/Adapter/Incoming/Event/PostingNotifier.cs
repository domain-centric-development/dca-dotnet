using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Listeners.Good.Infrastructure.Events;
using DomainCentric.ArchRules.Tests.Fixtures.Listeners.Good.Ledger.Application.NotifyPosting;

namespace DomainCentric.ArchRules.Tests.Fixtures.Listeners.Good.Ledger.Adapter.Incoming.Event;

/// <summary>A transactional listener that drives a use case.</summary>
public sealed class PostingNotifier : ITransactionalEventListener
{
    private readonly INotifyPostingInputPort _notifyPosting;

    public PostingNotifier(INotifyPostingInputPort notifyPosting) => _notifyPosting = notifyPosting;

    public Task OnAsync(object @event, CancellationToken cancellationToken = default) =>
        _notifyPosting.ExecuteAsync(new NotifyPostingCommand(@event.ToString() ?? ""), cancellationToken);
}
