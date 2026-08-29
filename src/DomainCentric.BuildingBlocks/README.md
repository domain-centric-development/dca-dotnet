# DomainCentric.BuildingBlocks

Building blocks of **Domain-Centric Architecture (DCA)** — a synthesis of Domain-Driven Design,
Hexagonal Architecture and Clean Architecture. The package contains marker interfaces, small base
contracts and attributes that make architectural roles *explicit in the type system*: an aggregate
root is an `IAggregateRoot<,>`, a use case is an `IUseCase<,>`, a bounded context is declared with
`[BoundedContext]`. Architecture rules and tooling (such as the companion `DomainCentric.ArchRules`
package) discover these roles by type, not by naming convention alone.

The package has **zero dependencies** and targets `netstandard2.1` and `net8.0`.

## Namespaces

### `DomainCentric.BuildingBlocks.Ddd.Tactical` — DDD Tactical Patterns

Marker interfaces for the Domain-Driven Design tactical patterns that guide domain model
implementation: `IId` (identity value objects), `IEntity<,>` (objects with identity), `IValue`
(immutable objects defined by attributes), `IAggregateRoot<,>` (entry point to aggregate clusters,
with `AggregateRootBase<,>` as a ready-made event-collecting base class), `IDomainEvent` (important
domain occurrences), `IIntegrationEvent` + `[IntegrationEventType]` (versioned cross-context events),
`IDomainService` (stateless domain operations), `IDomainGateway` (domain-owned port to external
facts), `IFactory` (complex object creation) and `ISpecification<>` (business rules as objects).
References: Eric Evans, *Domain-Driven Design* (2003); Vaughn Vernon, *Implementing Domain-Driven
Design* (2013).

### `DomainCentric.BuildingBlocks.Ddd.Strategic` — Context Boundaries

Attributes for the strategic patterns that define boundaries and relationships between domain
models: `[BoundedContext]` (explicit boundary for a domain model), and in the `Relationships`
sub-namespace `[SharedKernel]` (curated subset shared between contexts), `[OpenHostService]` (public
API exposed to other contexts), `[Upstream]`, `[ExternalUpstream]` and `[Partnership]` (the directed
and governance relationships that make up a context map). These declarations document architectural
boundaries and feed the context-map rules and renderer. References: Evans, Strategic Design chapters;
Vernon, Context Mapping.

### `DomainCentric.BuildingBlocks.Hexagonal.Ports.In` — Input Ports (Driving/Primary)

Input ports are the entry points to the application layer. They define how the outside world
(driving adapters: controllers, GraphQL resolvers, CLI handlers, event consumers, scheduled tasks,
MCP tool providers) can interact with the application.

```
IInputPort (marker)
  └── IUseCase<TInput, TOutput>
        └── I*InputPort : IUseCase<Command, Result>
```

### `DomainCentric.BuildingBlocks.Hexagonal.Ports.Out` — Output Ports (Driven/Secondary)

Output ports define what the application needs from the outside world — dependencies the
application layer requires but does not implement itself. Driven adapters implement them: repository
implementations, external API clients, message publishers, e-mail/SMS services, file storage,
identity services.

```
IOutputPort (marker)
  ├── IRepository<TAggregate, TId>
  ├── IStore
  ├── IDomainEventPublisher
  └── IIntegrationEventPublisher
```

Context-specific output ports (a token service, an identity session, …) are defined in their own
bounded context under `Application.Shared`, extending `IOutputPort` or `IStore`.

```
[Driving Adapters] → [Input Ports] → [Application] → [Output Ports] → [Driven Adapters]
(REST, Web, CLI)     (IUseCase)       (Domain)        (IRepository)    (DB, APIs, MQ)
```

## .NET conventions

* **`I` prefix.** All marker and contract interfaces follow the .NET convention: `IEntity`,
  `IAggregateRoot`, `IUseCase`, `IRepository`.
* **Records for values and events.** Value objects (`IValue`) and events (`IDomainEvent`,
  `IIntegrationEvent`) are `record`s; identifiers (`IId`) are `readonly record struct`s. The
  compiler provides value equality and immutability.
* **Non-generic bases for reflection.** `IEntity`, `IAggregateRoot` and `IRepository` exist as
  empty (or minimal) non-generic bases so rules can test `IsAssignableFrom` without closing the
  generic definitions. Domain code always implements the generic variants.
* **Async ports, synchronous domain.** `IUseCase<,>.ExecuteAsync`, `IRepository<,>` and the
  publishers are `Task`-based and take a `CancellationToken`. There is no synchronous twin. The
  domain layer itself (aggregates, entities, values, domain services) stays synchronous.
* **Context marker class instead of a package annotation.** C# has no namespace-level attributes.
  A bounded context (or the shared kernel) is declared by placing the attribute on one class that
  resides directly in the context's root namespace. The rules discover contexts by finding such a
  class:

  ```csharp
  namespace Acme.Shop.Cart;

  [BoundedContext("Shopping Cart", Description = "Carts and their items before checkout")]
  [Upstream("Product", Translation.AntiCorruptionLayer, Consumes.Api)]
  [Partnership("Checkout", Rationale = "ICartCompletionTrigger contract evolves jointly")]
  public static class CartContext
  {
  }
  ```

* **`AllowMultiple` instead of container attributes.** Repeatable relationship attributes
  (`[Upstream]`, `[ExternalUpstream]`, `[Partnership]`) are declared with `AllowMultiple = true`;
  no container types are needed.
* **Nested enums become top-level enums.** `UpstreamStatus`, `Translation`, `Consumes` and
  `Interaction` live directly in `Ddd.Strategic.Relationships`.

## Java ↔ C# mapping

This package is a port of the Java library `dev.domaincentric:dca-building-blocks`.

| Java (`dev.domaincentric.dca.buildingblocks`) | C# (`DomainCentric.BuildingBlocks`) |
|---|---|
| `ddd.tactical.Id` | `Ddd.Tactical.IId` |
| `ddd.tactical.Value` | `Ddd.Tactical.IValue` |
| `ddd.tactical.Entity<T, ID>` | `Ddd.Tactical.IEntity` (non-generic base) + `IEntity<TSelf, TId>` |
| `ddd.tactical.AggregateRoot<T, ID>` | `Ddd.Tactical.IAggregateRoot` (non-generic base) + `IAggregateRoot<TSelf, TId>` |
| `ddd.tactical.BaseAggregateRoot<T, ID>` | `Ddd.Tactical.AggregateRootBase<TSelf, TId>` |
| `ddd.tactical.DomainEvent` (`eventId()`, `occurredOn()`) | `Ddd.Tactical.IDomainEvent` (`EventId`, `OccurredOn`) |
| `ddd.tactical.IntegrationEvent` | `Ddd.Tactical.IIntegrationEvent` |
| `@IntegrationEventType(name, version)` | `[IntegrationEventType(name, Version = …)]` |
| `ddd.tactical.DomainService` | `Ddd.Tactical.IDomainService` |
| `ddd.tactical.DomainGateway` | `Ddd.Tactical.IDomainGateway` |
| `ddd.tactical.Factory` | `Ddd.Tactical.IFactory` |
| `ddd.tactical.Specification<T>` | `Ddd.Tactical.ISpecification<T>` |
| `@BoundedContext(name, description)` on `package-info` | `[BoundedContext(name, Description = …)]` on a context marker class |
| `@SharedKernel(description)` on `package-info` | `[SharedKernel(Description = …)]` on a marker class |
| `@OpenHostService(context, description)` | `[OpenHostService(context, Description = …)]` |
| `@Upstream(context, translation, via)` | `[Upstream(context, translation, params via)]` |
| `@Upstreams` (container) | — (`AllowMultiple = true`) |
| `@ExternalUpstream(name, translation, interaction, contractPackages, …)` | `[ExternalUpstream(name, translation, interaction) { ContractNamespaces, Protocol, Exchanges, Rationale, Status }]` |
| `@ExternalUpstreams` (container) | — (`AllowMultiple = true`) |
| `@Partnership(context, rationale)` | `[Partnership(context, Rationale = …)]` |
| `@Partnerships` (container) | — (`AllowMultiple = true`) |
| `Upstream.Status { IMPLEMENTED, PLANNED }` | `UpstreamStatus { Implemented, Planned }` |
| `Upstream.Translation { ANTI_CORRUPTION_LAYER, CONFORMIST }` | `Translation { AntiCorruptionLayer, Conformist }` |
| `Upstream.Consumes { API, EVENTS }` | `Consumes { Api, Events }` |
| `ExternalUpstream.Interaction { OUTBOUND, INBOUND }` | `Interaction { Outbound, Inbound }` |
| `hexagonal.port.in.InputPort` | `Hexagonal.Ports.In.IInputPort` |
| `hexagonal.port.in.UseCase<INPUT, OUTPUT>` (`execute`) | `Hexagonal.Ports.In.IUseCase<TInput, TOutput>` (`ExecuteAsync`) |
| `hexagonal.port.out.OutputPort` | `Hexagonal.Ports.Out.IOutputPort` |
| `hexagonal.port.out.Repository<T, ID>` (`findById`/`save`/`deleteById`) | `Hexagonal.Ports.Out.IRepository` (base) + `IRepository<TAggregate, TId>` (`FindByIdAsync`/`SaveAsync`/`DeleteByIdAsync`) |
| `hexagonal.port.out.Store` | `Hexagonal.Ports.Out.IStore` |
| `hexagonal.port.out.DomainEventPublisher` | `Hexagonal.Ports.Out.IDomainEventPublisher` (`PublishAsync`, `PublishAndClearEventsAsync`) |
| `hexagonal.port.out.IntegrationEventPublisher` | `Hexagonal.Ports.Out.IIntegrationEventPublisher` (`PublishAsync`) |
| `java.util.UUID` / `java.time.Instant` | `System.Guid` / `System.DateTimeOffset` |
| `Optional<T>` | nullable reference `T?` |

## License

MIT
