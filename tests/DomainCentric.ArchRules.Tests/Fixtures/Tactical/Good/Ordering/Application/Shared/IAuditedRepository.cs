using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good.Ordering.Application.Shared;

/// <summary>
/// A generic intermediate port: it binds no aggregate of its own, so DCA-TAC-016 skips it instead of
/// looking for a type named Audited.
/// </summary>
public interface IAuditedRepository<TAggregate, TId> : IRepository<TAggregate, TId>
    where TAggregate : class, IAggregateRoot<TAggregate, TId>
    where TId : IId
{
}
