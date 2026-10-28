using System.Threading;
using System.Threading.Tasks;
using DomainCentric.ArchRules.Tests.Fixtures.Listeners.Bad.Infrastructure.Events;
using DomainCentric.ArchRules.Tests.Fixtures.Listeners.Bad.Ledger.Application.NotifyPosting;

namespace DomainCentric.ArchRules.Tests.Fixtures.Listeners.Bad.Ledger.Adapter.Incoming.Event;

/// <summary>A synchronous listener that drives a use case through a helper.</summary>
public sealed class PostingNotifier : IEventListener
{
    private readonly INotifyPostingInputPort _notifyPosting;

    public PostingNotifier(INotifyPostingInputPort notifyPosting) => _notifyPosting = notifyPosting;

    public async Task OnAsync(object @event, CancellationToken cancellationToken = default) =>
        await SendAsync(@event.ToString() ?? "", cancellationToken).ConfigureAwait(false);

    private Task<NotifyPostingResult> SendAsync(string reference, CancellationToken cancellationToken) =>
        _notifyPosting.ExecuteAsync(new NotifyPostingCommand(reference), cancellationToken);
}
