using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DomainCentric.ArchRules.Tests.Fixtures.Listeners.Good.Ledger.Application.NotifyPosting;

public sealed record NotifyPostingCommand(string Reference);

public sealed record NotifyPostingResult(bool Notified);

public interface INotifyPostingInputPort : IUseCase<NotifyPostingCommand, NotifyPostingResult>
{
}

public sealed class NotifyPostingUseCase : INotifyPostingInputPort
{
    public Task<NotifyPostingResult> ExecuteAsync(NotifyPostingCommand input, CancellationToken cancellationToken = default) =>
        Task.FromResult(new NotifyPostingResult(true));
}
