using System.Threading;
using System.Threading.Tasks;

namespace DomainCentric.ArchRules.Tests.Fixtures.Listeners.Good.Infrastructure.Events;

public interface IEventListener
{
    Task OnAsync(object @event, CancellationToken cancellationToken = default);
}

public interface ITransactionalEventListener : IEventListener
{
}
