// DCA-USE-009 with a project's own aggregate-root vocabulary. Its RegisterEvent is public, so a
// class outside the aggregate's hierarchy can call it — the case the external-registration scan
// exists for. With the library's own marker the method is protected and only a nested type can
// reach it, and a nested type already belongs to the enclosing type's call graph.
using System;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.OwnEvents.Vocabulary
{
    /// <summary>A project's own aggregate-root base: the registration is part of its public surface.</summary>
    public abstract class OwnAggregateBase<TSelf, TId> : IAggregateRoot<TSelf, TId>
        where TSelf : OwnAggregateBase<TSelf, TId>
        where TId : IId
    {
        private readonly System.Collections.Generic.List<IDomainEvent> _events = new();

        public abstract TId Id { get; }

        public System.Collections.Generic.IReadOnlyList<IDomainEvent> DomainEvents => _events;

        public void ClearDomainEvents() => _events.Clear();

        public void RegisterEvent(IDomainEvent @event) => _events.Add(@event);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.OwnEvents.Module
{
    [BoundedContext("Module", Description = "own event vocabulary")]
    public static class ModuleContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.OwnEvents.Module.Domain.Model
{
    using DomainCentric.ArchRules.Tests.Fixtures.OwnEvents.Vocabulary;

    public readonly record struct TicketId(Guid Value) : IId;

    public sealed record Raised(Guid EventId, DateTimeOffset OccurredOn) : IDomainEvent;

    /// <summary>Registers nothing in its own members.</summary>
    public sealed class Ticket : OwnAggregateBase<Ticket, TicketId>
    {
        public Ticket(TicketId id) => Id = id;

        public override TicketId Id { get; }
    }

    /// <summary>Outside the aggregate's hierarchy, and registers on it.</summary>
    public sealed class TicketRegistrar
    {
        public void Mark(Ticket ticket) => ticket.RegisterEvent(new Raised(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.OwnEvents.Module.Application.Shared
{
    using DomainCentric.ArchRules.Tests.Fixtures.OwnEvents.Module.Domain.Model;

    public interface ITicketRepository : IRepository<Ticket, TicketId>
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.OwnEvents.Module.Application.SaveTicket
{
    using DomainCentric.ArchRules.Tests.Fixtures.OwnEvents.Module.Application.Shared;
    using DomainCentric.ArchRules.Tests.Fixtures.OwnEvents.Module.Domain.Model;

    public sealed class SaveTicketUseCase
    {
        private readonly ITicketRepository _tickets = null!;

        public Task ExecuteAsync(Ticket ticket, CancellationToken cancellationToken = default) =>
            _tickets.SaveAsync(ticket, cancellationToken);
    }
}
