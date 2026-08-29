# DCA rule catalog (.NET)

Generated from `DomainCentric.ArchRules` — do not edit. 110 rules in 11 sets; 2 Java rules not applicable in .NET.

## `layered`

| Id | Rule | Rationale |
|----|------|-----------|
| `DCA-LAY-001` | Diagnostic: The rules of the Layered Architecture should be followed | Traditional layering (application accessed only by incoming adapters) contradicts Ports and Adapters, where outgoing adapters implement application-level output ports; the hexagonal rules cover the intended dependency direction |
| `DCA-LAY-002` | Domain must not have dependencies on Infrastructure | Domain should not depend on infrastructure concerns (Dependency Inversion Principle) |
| `DCA-LAY-003` | Application Services must only use outbound ports (not infrastructure implementations) | Application services should only use outbound ports declared as interfaces (Ports.Out), not infrastructure implementation details |
| `DCA-LAY-004` | Transaction boundaries belong to the application layer | Transactions are an application-layer concern - domain and incoming adapters must not manage them |
| `DCA-LAY-005` | The shared kernel's output-port markers must all be interfaces | Ports.Out contains outbound port interfaces (IRepository, IOutputPort, IDomainEventPublisher) shared across all bounded contexts. These must be interfaces to ensure the application layer remains framework-independent and follows the Dependency Inversion Principle. Implementations belong in infrastructure or adapter namespaces. |

## `onion`

| Id | Rule | Rationale |
|----|------|-----------|
| `DCA-ONI-001` | Domain must not access Application Services (Onion Architecture - Domain is innermost layer) | Domain is the innermost layer in onion architecture and should not depend on application services |
| `DCA-ONI-002` | The Domain Model should be framework independent and should not use 3rd party libraries when possible | Domain should be framework-independent (Dependency Inversion Principle) |
| `DCA-ONI-003` | Domain Models must not have framework attributes | Domain models must be framework-independent (no DI-container or ORM attributes) |

## `hexagonal`

| Id | Rule | Rationale |
|----|------|-----------|
| `DCA-HEX-001` | Classes from the domain should not access port adapters | Domain should not depend on adapters (ports and adapters pattern) |
| `DCA-HEX-002` | Application Services should not access port adapters | Application services should only depend on domain and outbound ports, not adapters |
| `DCA-HEX-003` | Controllers and Resources must never access repositories directly | Controllers must go through use cases (input ports), never directly to repositories |
| `DCA-HEX-004` | Incoming Adapters must only use outbound ports (not infrastructure implementations) | Incoming adapters should only use outbound ports declared as interfaces (Ports.Out), not infrastructure implementation details |
| `DCA-HEX-005` | Outgoing Adapters must only use outbound ports (not infrastructure implementations) | Outgoing adapters should only use outbound ports declared as interfaces (Ports.Out), not infrastructure implementation details |
| `DCA-HEX-006` | Port adapters (incoming and outgoing) must not communicate directly with each other within the same context | Port adapters should communicate through application services, not directly (event consumers are the exception) |
| `DCA-HEX-007` | Incoming adapters must only access their own bounded context (except event consumers and Open Host Services) | Incoming adapters must only orchestrate use cases from their own bounded context - use domain events for cross-context integration |
| `DCA-HEX-008` | Classes named *Repository must reside in the outgoing adapter namespace | Repository implementations are secondary adapters (outgoing ports) |
| `DCA-HEX-009` | Output Ports in Application.Shared must extend IOutputPort | Top-level interfaces in Application.Shared are output ports and must extend IOutputPort to be part of the port hierarchy. Nested interfaces (e.g. IIdentityProvider.Identity) are part of their enclosing port's contract, not ports themselves |
| `DCA-HEX-010` | Output ports must not reside in the domain layer | output ports (IRepository, IStore, IOutputPort) are an application-layer concern and must live in Application/Shared/, not Domain/ |

## `tactical`

| Id | Rule | Rationale |
|----|------|-----------|
| `DCA-TAC-001` | Aggregate Roots must implement IAggregateRoot | Classes named *AggregateRoot must implement the IAggregateRoot interface (DDD pattern) |
| `DCA-TAC-002` | Aggregate Roots must not hold references to Repositories or other Output Ports | Aggregates are persistence-ignorant: repositories and services are passed as method parameters by the use case, never injected as fields |
| `DCA-TAC-003` | Aggregate Roots must not have fields with other Aggregate Root types | Vernon's Aggregate Design Rule #2: reference other Aggregates by identity to keep aggregate boundaries and transactional consistency intact |
| `DCA-TAC-004` | Entities must have an ID field | An Entity is defined by its identity, which is a value object implementing the IId marker |
| `DCA-TAC-005` | Entities must not be instantiated directly from outside the aggregate | Entities are created through their aggregate root so that the root can enforce its invariants |
| `DCA-TAC-006` | Domain model classes must not have public setter methods | Behavior-rich domain models change state through intention-revealing methods from the ubiquitous language, never through public property setters |
| `DCA-TAC-007` | Entities must not have fields with Aggregate Root types | An entity inside an aggregate references other aggregates by identity only, otherwise the aggregate boundary leaks |
| `DCA-TAC-008` | Value Objects must not contain Aggregate Roots or Entities | A Value Object is defined by its attributes; holding an object with identity would give it a lifecycle it must not have |
| `DCA-TAC-009` | Value Object classes should be sealed (immutability) | Value objects should be immutable and not extensible - Vernon's DDD recommendation; .NET: a sealed class, a sealed record, or a struct |
| `DCA-TAC-010` | Value Object fields must be readonly (deep immutability) | Records have implicitly init-only state and enums are immutable by design; a hand-written value class must make every instance field readonly and every property get-only or init-only itself |
| `DCA-TAC-011` | Value Objects must not have setter methods | Value Objects are immutable; state changes produce a new instance instead of mutating |
| `DCA-TAC-012` | Value Objects must be records or immutable classes with attribute equality | A record grants attribute-based equality for free; a hand-written Value Object class must override Equals and GetHashCode itself to compare by its attributes |
| `DCA-TAC-013` | Repository Interfaces should extend the IRepository Marker Interface | Repository interfaces should extend the IRepository marker interface |
| `DCA-TAC-014` | Repository interfaces must reside in the application layer's shared output-port namespace | Repository interfaces are output ports in the application layer (Hexagonal Architecture) |
| `DCA-TAC-015` | Repository Implementations must reside in the Adapter.Outgoing namespace | Repository implementations are outgoing adapters in bounded contexts |
| `DCA-TAC-016` | Repositories must only exist for Aggregate Roots | A repository is the collection of one aggregate type; a repository for an entity would let callers bypass the root that guards the aggregate's invariants |
| `DCA-TAC-017` | Repository methods must not return non-root Entities | A caller receiving an Entity that is not an Aggregate Root could mutate part of an aggregate without passing its root, so the root's invariants would never run |
| `DCA-TAC-018` | Store interfaces must extend the IStore marker, not IRepository | Stores extend the IStore marker; IRepository is reserved for Aggregate Roots |
| `DCA-TAC-019` | Store interfaces must reside in the application layer's shared output-port namespace | Store interfaces are output ports in the application layer (Hexagonal Architecture) |
| `DCA-TAC-020` | Store implementations must reside in the Adapter.Outgoing namespace | Store implementations are outgoing adapters in bounded contexts |
| `DCA-TAC-021` | Store interfaces must not declare FindById or Save methods | FindById/Save are Repository semantics; a Store that has them is a Repository wearing the wrong name, and the stored object should then be an Aggregate Root |
| `DCA-TAC-022` | Enriched Domain Models must be Value Object records | Enriched domain models are immutable read projections and must be records implementing IValue |

## `strategic`

| Id | Rule | Rationale |
|----|------|-----------|
| `DCA-STR-001` | Diagnostic: Display discovered bounded contexts | Making the discovered contexts visible shows which namespaces the strategic rules govern |
| `DCA-STR-002` | Shared Kernel must not have dependencies on any bounded context | Shared Kernel must be context-independent — it is shared by all contexts and owned by none |
| `DCA-STR-003` | Bounded contexts must not directly access each other in application layer (except allowed dependencies) | Application layers talk to other contexts through output ports and adapters, never directly |
| `DCA-STR-004` | Bounded contexts must not access each other in the domain layer | A domain layer talks to its own context and the shared kernel, nothing else — not even another context's Api/ |
| `DCA-STR-005` | Open Host Services must reside in Api or Adapter.Incoming.OpenHost namespaces | Open Host Services expose context capabilities via Api/ namespaces (published named interface) or Adapter.Incoming.OpenHost/ namespaces |
| `DCA-STR-006` | Outgoing adapters accessing other contexts must only use OpenHostService classes (except allowed ACL patterns) | Cross-context communication goes through the published Api/ and Events/ namespaces, never through another context's domain or application layer |
| `DCA-STR-007` | Integration Events must be in Events or adapter outgoing event namespaces | Integration Events must be in Events/ namespaces (published named interface) or Adapter.Outgoing.Event/ namespaces |
| `DCA-STR-008` | Integration Events should be immutable records | Integration Events must be immutable to ensure event integrity across contexts (Event Sourcing best practice) |
| `DCA-STR-009` | Anti-Corruption Layer components must be in Acl namespaces | Anti-Corruption Layer components must be in 'Acl' namespaces for clear architectural intent (DDD Strategic Pattern) |
| `DCA-STR-010` | Event Listeners consuming integration events should use Anti-Corruption Layer | Consumed integration events are translated into the consuming context's own language before they reach its domain — verified by code review, not statically |

## `contextmap`

| Id | Rule | Rationale |
|----|------|-----------|
| `DCA-MAP-001` | Upstream, ExternalUpstream, and Partnership may only be declared on bounded context namespaces | Context map declarations are reserved for bounded contexts — only a context can be downstream of, or partner with, another |
| `DCA-MAP-002` | ExternalUpstream declarations must be well-formed and unique per name and interaction | The identity of an [ExternalUpstream] declaration is (name, interaction); internal contexts are declared with [Upstream] instead |
| `DCA-MAP-003` | Distinct external system names must not collide after mermaid id normalization | The generated context map renders one node per normalized external system name — two spellings of the same system would silently merge into one node |
| `DCA-MAP-004` | Upstream declarations must reference an existing bounded context and never the declaring context itself | A dangling or self-referencing upstream edge describes a relationship that cannot exist |
| `DCA-MAP-005` | Upstream declarations must be unique per context and channel, and via must not be empty | The identity of an [Upstream] declaration is (context, via); different translations per channel require separate attributes |
| `DCA-MAP-007` | Implemented Upstream declarations must be backed by an actual code dependency | A declared Implemented edge without any real dependency is stale (or premature — then it is Planned) and would otherwise pass forever alongside an equally stale module boundary entry |
| `DCA-MAP-008` | Anti-Corruption Layer: upstream contract types must stay inside the matching adapter | The ACL sits where the dependency crosses the boundary — outgoing adapters for synchronous API calls, incoming adapters for consumed events — and translates the upstream contract into the context's own model there |
| `DCA-MAP-009` | Conformist: upstream contract types must never reach the domain layer | Conformism does not suspend domain purity — the domain layer stays free of foreign contract types |
| `DCA-MAP-010` | External system contract types must respect the declared translation and interaction | An external system's contract types are confined to the adapter where the exchange crosses the boundary (ACL) or at least kept out of the domain (Conformist) |
| `DCA-MAP-011` | Cross-context dependencies on published interfaces require an Upstream declaration | Every real dependency on a foreign Api/ or Events/ namespace is a context-map edge and must be declared as such |
| `DCA-MAP-012` | Partnership declarations must reference an existing bounded context, never themselves, and must be symmetric | A partnership is a mutual commitment — it exists only when both contexts declare it |
| `DCA-MAP-013` | Diagnostic: Display declared context map | Printing the declared edges makes the executable context map reviewable at a glance |

Not applicable in .NET:

- `DCA-MAP-006` — Upstream declarations and Spring Modulith allowedDependencies must agree — .NET has no module system annotation; project boundaries take that role

## `advanced`

| Id | Rule | Rationale |
|----|------|-----------|
| `DCA-ADV-001` | Domain Events must implement IDomainEvent Marker Interface and be records | Domain events should be immutable records implementing IDomainEvent (named in past tense, e.g., ProductCreated, CartCleared) |
| `DCA-ADV-002` | Domain Events must reside in domain namespace | Domain events are part of the domain layer (named in past tense) |
| `DCA-ADV-003` | Domain Events should be immutable (sealed or records) | Domain events should be immutable (sealed classes or records) |
| `DCA-ADV-004` | Domain Events must not have framework attributes | Domain events must be framework-independent plain objects |
| `DCA-ADV-005` | Integration Events must be annotated with IntegrationEventType | [IntegrationEventType(name, Version)] is the contract identity of every integration event — the serializer keys (name, version) to the class and stamps both onto the wire envelope |
| `DCA-ADV-006` | Integration Events must not have a version field | The schema version is a class property ([IntegrationEventType]), never per-instance payload data — a version data field duplicates the attribute and can drift from it |
| `DCA-ADV-007` | Domain Events that are not Integration Events must not have a version field | Versioning is a contract concern of integration events — a purely internal domain event has no wire contract to version |
| `DCA-ADV-008` | Domain Events must have a timestamp field | An event records something that happened — without a timestamp the fact cannot be ordered, replayed or audited |
| `DCA-ADV-009` | Domain Services must implement IDomainService Marker Interface and reside in Domain.Service | Domain services implement IDomainService marker and reside in Domain.Service namespaces (named descriptively, e.g., PricingService, CartTotalCalculator) |
| `DCA-ADV-010` | Domain Services must reside in domain namespace | Domain services are part of the domain layer, not application layer |
| `DCA-ADV-011` | Domain Services must not have framework attributes | Domain services should be framework-independent |
| `DCA-ADV-012` | Domain Services should be stateless (only readonly fields for dependencies) | Domain services should be stateless (only readonly fields for dependencies) |
| `DCA-ADV-013` | Factories should implement IFactory Marker Interface | Classes implementing IFactory marker should have 'Factory' in their name |
| `DCA-ADV-014` | Factories must reside in domain namespace | Factories are part of the domain layer (complex aggregate creation logic) |
| `DCA-ADV-015` | Factories must not have framework attributes | Factories should be framework-independent |
| `DCA-ADV-016` | Factories should be stateless (only readonly fields for dependencies) | Factories should be stateless (only readonly fields for dependencies) |
| `DCA-ADV-017` | Specifications must end with 'Specification' | Specification implementations are part of the domain layer |
| `DCA-ADV-018` | Specifications must not have framework attributes | Specifications should be framework-independent value objects |

## `usecase`

| Id | Rule | Rationale |
|----|------|-----------|
| `DCA-USE-001` | Base IInputPort interface must be in the building-blocks port in namespace | Base IInputPort interface defines the generic contract for all use cases (Hexagonal Architecture) |
| `DCA-USE-002` | Use Case Commands must end with 'Command' and reside in application namespace | Use case commands should be in application layer (CQRS pattern) |
| `DCA-USE-003` | Use Case Queries must end with 'Query' and reside in application namespace | Use case queries should be in application layer (CQRS pattern) |
| `DCA-USE-004` | Use Case Commands should be immutable (sealed or records) | Use case commands should be immutable (value objects) |
| `DCA-USE-005` | Use Case Queries should be immutable (sealed or records) | Use case queries should be immutable (value objects) |
| `DCA-USE-006` | Use Case Result Models must end with 'Result' and reside in application namespace | Use case result models should be in application layer. Domain Value Objects with 'Result' in name are allowed in domain layer. |
| `DCA-USE-007` | Use Case Result Models should be immutable (sealed or records) | Use case result models should be immutable (value objects) |
| `DCA-USE-008` | HTTP Response Models must end with 'Response' and reside in adapter incoming namespace | HTTP response models should be in adapter incoming layer |
| `DCA-USE-009` | Use cases that save an aggregate must publish its domain events | A saved aggregate must not keep its events: unpublished, they are lost, and stored on the instance they may later be published out of context. Publishing belongs after the save, in the use case that owns the unit of work - even when the action raised no event |
| `DCA-USE-010` | DTOs must not be used in the Domain Layer | Domain layer should not depend on DTOs (presentation concerns) - Dependency Inversion Principle |
| `DCA-USE-011` | DTOs must not be used in the Application Layer | Application layer should use Command/Query/Response models, not presentation DTOs (Clean Architecture) |

## `naming`

| Id | Rule | Rationale |
|----|------|-----------|
| `DCA-NAM-001` | Application layer InputPort implementations must end with 'UseCase' | InputPort implementations (use cases) should follow consistent naming conventions (Hexagonal Architecture) |
| `DCA-NAM-003` | InputPort interfaces must end with 'InputPort' | Input port interfaces should follow consistent naming conventions (Hexagonal Architecture) |
| `DCA-NAM-004` | Repository Interfaces must end with 'Repository' | Repository interfaces should follow consistent naming conventions (DDD pattern) |
| `DCA-NAM-005` | Controller classes must end with 'Controller' | MVC controller and page model classes should follow naming conventions |
| `DCA-NAM-006` | REST Controllers must end with 'Controller' (REST best practice) | [ApiController] classes should end with 'Controller' following RESTful naming conventions |
| `DCA-NAM-007` | DTOs must reside in the adapter layer, not in domain or application | DTOs are adapter concerns (presentation or external API) - not in domain or application |
| `DCA-NAM-008` | Converters must reside in the adapter layer | Converters/Mappers translate between layers and should be in adapters |
| `DCA-NAM-009` | No technical bucket namespaces - namespace by domain concept | Namespaces are named after domain concepts from the ubiquitous language, not technical patterns |
| `DCA-NAM-010` | Domain classes must not use technical suffixes (Manager, Helper, Util, Impl) | Domain names come from the ubiquitous language - name services by their specialty, not by technical role |
| `DCA-NAM-011` | ViewModels must reside in Adapter.Incoming.Web namespaces | ViewModels are presentation concerns and must reside in incoming web adapter namespaces |

Not applicable in .NET:

- `DCA-NAM-002` — .NET has no @Service stereotype — use cases are registered in the DI container by code, there is no attribute to check

## `cycles`

| Id | Rule | Rationale |
|----|------|-----------|
| `DCA-CYC-001` | Domain Namespaces must not have cyclic dependencies | Domain model namespaces should have clear boundaries and no cycles (Acyclic Dependencies Principle) |
| `DCA-CYC-002` | Application Layer must not have cyclic dependencies | Application services should have clear boundaries and no cycles |
| `DCA-CYC-003` | Outgoing Adapter Namespaces must not have cyclic dependencies | Outgoing adapters should have clear boundaries and no cycles |
| `DCA-CYC-004` | Incoming Adapter Namespaces must not have cyclic dependencies | Incoming adapters should have clear boundaries and no cycles |

## `dotnet`

| Id | Rule | Rationale |
|----|------|-----------|
| `DCA-NET-001` | Domain layer must stay synchronous | Async is an I/O concern of ports and adapters; a synchronous domain model stays testable, deterministic and free of sync-over-async hazards |
| `DCA-NET-002` | Port methods returning Task must end with Async | The Async suffix is the .NET convention that tells callers a method is awaitable; ports are the contract other layers program against |
| `DCA-NET-003` | Use cases must expose exactly one ExecuteAsync | One use case, one entry point: the input port is the only way in, and a cancellation token lets the host stop long-running work |
| `DCA-NET-004` | Value objects should be records or readonly record structs | Records give attribute-based equality, immutability by default and with-expressions — the C# way to write a Value Object |
| `DCA-NET-005` | Identifiers should be readonly record structs | A strongly typed identifier as a readonly record struct costs no allocation and cannot be confused with a raw Guid or string |

