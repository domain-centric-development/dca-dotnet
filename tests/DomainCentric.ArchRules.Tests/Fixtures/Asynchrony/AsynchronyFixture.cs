// Fixture for DCA-ONI-004 — a synchronous and an asynchronous domain.
using System.Threading;
using System.Threading.Tasks;

namespace DomainCentric.ArchRules.Tests.Fixtures.Asynchrony.Bad.Ledger.Domain
{
    // Hands back a task: the domain has become asynchronous. Reported.
    public class Ledger
    {
        private int balance;

        public Task<int> BalanceLaterAsync() => Task.FromResult(balance);
    }

    // Takes a cancellation token. Reported.
    public class PostingFeed
    {
        public int Next(CancellationToken cancellationToken) => cancellationToken.IsCancellationRequested ? 0 : 1;
    }

    // A code base's own awaitable - not one of the configured asynchronous types by default.
    public class Deferred<T>
    {
        public Deferred(T value)
        {
            Value = value;
        }

        public T Value { get; }
    }

    // Uses the code base's own awaitable: not reported unless the layout names it.
    public class Posting
    {
        public Deferred<int> AmountLater() => new(0);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Asynchrony.Bad.Ledger.Application
{
    // Asynchrony in the application layer is not this rule's subject. Not reported.
    public class RecordPostingUseCase
    {
        public Task ExecuteAsync(string posting, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Asynchrony.Good.Ledger.Domain
{
    // A synchronous domain model.
    public class Ledger
    {
        public int Balance { get; private set; }

        public void Post(int amount) => Balance += amount;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Asynchrony.Good.Ledger.Application
{
    using DomainCentric.ArchRules.Tests.Fixtures.Asynchrony.Good.Ledger.Domain;

    // Awaits in the application layer and passes plain values into the domain.
    public class RecordPostingUseCase
    {
        private readonly Ledger ledger = new();

        public Task<int> ExecuteAsync(int amount, CancellationToken cancellationToken)
        {
            ledger.Post(amount);
            return Task.FromResult(ledger.Balance);
        }
    }
}
