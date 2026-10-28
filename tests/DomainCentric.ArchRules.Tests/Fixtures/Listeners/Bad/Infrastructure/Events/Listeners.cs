using System.Threading;
using System.Threading.Tasks;

namespace DomainCentric.ArchRules.Tests.Fixtures.Listeners.Bad.Infrastructure.Events;

public interface IEventListener
{
    Task OnAsync(object @event, CancellationToken cancellationToken = default);
}

public interface ITransactionalEventListener : IEventListener
{
}
