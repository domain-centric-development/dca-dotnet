// Fixture for DCA-LAY-008 — a use case on a persistence API and one behind its output port.
using System.Data;

namespace DomainCentric.ArchRules.Tests.Fixtures.PersistenceApi.Bad.Vendor
{
    // Stands in for the session type of a persistence library the default list does not name.
    public interface ISession
    {
        void Flush();
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.PersistenceApi.Bad.Ledger.Application
{
    using DomainCentric.ArchRules.Tests.Fixtures.PersistenceApi.Bad.Vendor;

    // Runs its own transaction on a connection. Reported.
    public class RecordPostingUseCase
    {
        private readonly IDbConnection connection;

        public RecordPostingUseCase(IDbConnection connection)
        {
            this.connection = connection;
        }

        public void Execute(string posting)
        {
            using var transaction = connection.BeginTransaction();
            transaction.Commit();
        }
    }

    // Uses a persistence library outside the default list: not reported unless the layout names it.
    public class ClosePeriodUseCase
    {
        private readonly ISession session;

        public ClosePeriodUseCase(ISession session)
        {
            this.session = session;
        }

        public void Execute() => session.Flush();
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.PersistenceApi.Bad.Ledger.Adapter.Outgoing
{
    // An outgoing adapter may use the persistence API. Not reported.
    public class ConnectionPostings
    {
        public ConnectionPostings(IDbConnection connection)
        {
            Connection = connection;
        }

        public IDbConnection Connection { get; }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.PersistenceApi.Good.Ledger.Application
{
    // The output port the use case programs against.
    public interface IPostings
    {
        void Add(string posting);
    }

    // Talks to its output port only; the boundary is drawn around it.
    public class RecordPostingUseCase
    {
        private readonly IPostings postings;

        public RecordPostingUseCase(IPostings postings)
        {
            this.postings = postings;
        }

        public void Execute(string posting) => postings.Add(posting);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.PersistenceApi.Good.Ledger.Adapter.Outgoing
{
    using DomainCentric.ArchRules.Tests.Fixtures.PersistenceApi.Good.Ledger.Application;

    // Implements the port with the persistence API, where it belongs.
    public class ConnectionPostings : IPostings
    {
        private readonly IDbConnection connection;

        public ConnectionPostings(IDbConnection connection)
        {
            this.connection = connection;
        }

        public void Add(string posting)
        {
            using var command = connection.CreateCommand();
            command.CommandText = posting;
            command.ExecuteNonQuery();
        }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.PersistenceApi.Declarative.Vendor
{
    // Stands in for a persistence library's transactional attribute; the test names it as the transactional role.
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
    public sealed class TransactionalAttribute : System.Attribute
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.PersistenceApi.Declarative.Ledger.Application
{
    using System.Transactions;
    using DomainCentric.ArchRules.Tests.Fixtures.PersistenceApi.Declarative.Vendor;

    // Declares its transaction with the configured transactional attribute. Passes.
    public class CloseDayUseCase
    {
        [Transactional]
        public void Execute()
        {
        }
    }

    // Declares its transaction - that passes - and then draws one programmatically as well: the scope is reported.
    [Transactional]
    public class SettlePeriodUseCase
    {
        public void Execute()
        {
            using var scope = new TransactionScope();
            scope.Complete();
        }
    }
}
