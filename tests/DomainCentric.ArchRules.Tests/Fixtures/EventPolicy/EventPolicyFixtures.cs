using System; using System.Threading; using System.Threading.Tasks; using DomainCentric.BuildingBlocks.Ddd.Tactical; using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out; using DomainCentric.BuildingBlocks.Application.Transactions;
namespace DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Domain.Model {
public readonly record struct EntryId(Guid Value):IId;
public class Entry : AggregateRootBase<Entry,EntryId> { public Entry(EntryId id){Id=id;} public override EntryId Id{get;} }
public class Recorded : AggregateRootBase<Recorded,EntryId> {public override EntryId Id=>new(Guid.NewGuid()); public void Change()=>RecordChange(); private void RecordChange()=>RegisterEvent(new Changed(Guid.NewGuid(),DateTimeOffset.UtcNow,1));}
public sealed record Changed(Guid EventId,DateTimeOffset OccurredOn,int Version):IDomainEvent;
}
namespace DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Events { [IntegrationEventType("exported",Version=1)] public sealed record Exported(Guid EventId,DateTimeOffset OccurredOn,int Version):IIntegrationEvent; }
namespace DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Adapter.Outgoing.Event {public sealed record Misplaced(Guid EventId,DateTimeOffset OccurredOn):IIntegrationEvent;}
namespace DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Application.Shared {public interface IEntryRepository:IRepository<DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Domain.Model.Entry,DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Domain.Model.EntryId>{} public interface IRecordedRepository:IRepository<DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Domain.Model.Recorded,DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Domain.Model.EntryId>{}}
namespace DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Application.Free { public class SaveUseCase {private DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Application.Shared.IEntryRepository repo=null!; public Task ExecuteAsync(DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Domain.Model.Entry value)=>repo.SaveAsync(value); } }
namespace DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Application.Recording { public class SaveUseCase {private DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Application.Shared.IRecordedRepository repo=null!; public Task ExecuteAsync(DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Domain.Model.Recorded value)=>repo.SaveAsync(value); } }
namespace DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Application.AfterEmpty { public class AfterEmptyUseCase {private ITransactionBoundary boundary=null!; private IDomainEventPublisher events=null!; public async Task ExecuteAsync(DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Domain.Model.Recorded value){await boundary.InTransactionAsync(ct=>Task.CompletedTask);await events.PublishAndClearEventsAsync(value);} } }
namespace DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Application.Uncovered { public class UncoveredUseCase {private IDomainEventPublisher events=null!;public Task ExecuteAsync(DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Domain.Model.Recorded value)=>events.PublishAndClearEventsAsync(value);} }
namespace DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Application.Covered {public class CoveredUseCase {private ITransactionBoundary boundary=null!; private IDomainEventPublisher events=null!; public Task ExecuteAsync(DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Domain.Model.Recorded value)=>boundary.InTransactionAsync(ct=>events.PublishAndClearEventsAsync(value,ct));}}

namespace DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Application.Unresolved {
public class SaveUseCase<T> where T:class,IAggregateRoot<T,DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Domain.Model.EntryId> {
private IRepository<T,DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Domain.Model.EntryId> repo=null!;
public Task ExecuteAsync(T value)=>repo.SaveAsync(value);
}}
namespace DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Application.Declarative {
[AttributeUsage(AttributeTargets.Method)] public sealed class WorkAttribute:Attribute {}
public class AttributedUseCase {private IDomainEventPublisher events=null!;
[Work] public Task ExecuteAsync(DomainCentric.ArchRules.Tests.Fixtures.EventPolicy.Module.Domain.Model.Recorded value)=>events.PublishAndClearEventsAsync(value);
}}
