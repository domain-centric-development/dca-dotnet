using System;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Infrastructure.Transactions
{
    /// <summary>A project's own declarative transaction attribute; no .NET preset configures one.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class UnitOfWorkAttribute : Attribute
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Domain.Service
{
    using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Infrastructure.Transactions;

    /// <summary>
    /// DCA-LAY-004, declarative branch: a domain type carrying the configured transactional attribute
    /// draws a boundary where the domain must not. Reported only once the attribute is configured.
    /// </summary>
    [UnitOfWork]
    public sealed class DeclarativelyTransactionalPricing
    {
        public decimal Tax(decimal amount) => amount / 5;
    }
}
