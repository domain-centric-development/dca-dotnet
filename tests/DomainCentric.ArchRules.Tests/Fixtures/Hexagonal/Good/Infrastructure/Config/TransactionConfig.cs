namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Infrastructure.Config;

/// <summary>A project's own transaction-manager abstraction, configured through TransactionManagerTypes in the test.</summary>
public interface IUnitOfWorkManager
{
}

/// <summary>The composition root declares the transaction manager; it draws no boundary and is not a LAY-004 finding.</summary>
public static class TransactionConfig
{
    public static IUnitOfWorkManager? TransactionManager() => null;
}
