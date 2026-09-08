# Porting log — `dca-archunit` (Java) → `DomainCentric.ArchRules` (.NET)

Append-only. One section per rule set; the core section is written by the coordinator.

## Core

- `DcaLayout` mirrors the Java class; patterns are regular expressions (ArchUnitNET has no `..` syntax). Defaults are PascalCase namespace segments (`Domain`, `Application`, `Adapter.Incoming`, …), controller suffix `Controller` (ASP.NET), use-case suffix `UseCase`.
- `IDcaRule.Selects` / `IDcaRule.Checks` mirror Java's `selects()` / `checks()`; `DcaRule.Of`/`Check` return `DcaRule.Undescribed`, completed with `.Selecting(...).Checking(...)` (both mandatory). Texts follow the Java wording sentence by sentence and deviate only where the .NET reading differs (namespaces, `I`-prefixed markers, attributes, async ports - `PublishAndClearEventsAsync`); the rule catalog tool writes them as `selects`/`checks`, the knowledge catalog shows a **.NET reading** block wherever the texts differ.
- `FrameworkAnnotations` (Spring names) → `FrameworkTypes` (ASP.NET Core `ControllerBase`, `[ApiController]`, `PageModel`, `System.Transactions.TransactionScope`). .NET has no `@Service`/`@Component`/`@EventListener`/`@ApplicationModule` counterpart; the affected rules are n/a and listed per set.
- `package-info` annotations → attributes on a **marker class residing directly in the context root namespace** (`[BoundedContext("Cart")] public static class CartContext {}`); read by reflection (`DcaArchitecture.NamespaceAttribute(s)<T>`). Repeatable annotations → `AllowMultiple = true`, no container attributes.
- `DcaRule.of` → `DcaRule.Of` evaluates the ArchUnitNET rule; failed `EvaluationResult`s become violation lines; an empty selection passes (ArchUnitNET would fail it — "requires positive evaluation").
- Assertion type: `DcaRuleViolationException` (no test-framework dependency); xUnit integration lives in the separate package `DomainCentric.ArchRules.Xunit` (the Java `junit` sub-package is `compileOnly`; NuGet has no equivalent that keeps NUnit/MSTest users clean).
- New rule set `DotnetRules` (`DCA-NET-001…`) for rules that only make sense in .NET (domain stays synchronous, `Async` suffix on Task-returning port methods, use cases expose exactly one `ExecuteAsync`).
- **Release blind spot** (found by the hexagonal port, verified with a probe): ArchUnitNET 0.13.4 drops struct-based async state machines, which the compiler emits in optimized builds; dependencies used only inside `async` bodies then vanish from `Dependencies`. Debug builds emit class state machines whose dependencies ArchUnitNET merges into the declaring type. Decision: `DcaArchitecture.Load` refuses JIT-optimized assemblies (`InvalidOperationException` naming the assembly) unless `allowOptimizedAssemblies: true`; README, CI and the sample run architecture tests in Debug. Worth an upstream issue at TNG/ArchUnitNET.
- **Markers always loaded**: `DcaArchitecture.Load` also loads the `DomainCentric.BuildingBlocks` assembly into the ArchUnitNET model. ArchUnitNET resolves `typeof(IDomainService)` against the loaded architecture and throws `TypeDoesNotExistInArchitecture` when nothing references the marker — found when the minimal consumer (no domain service, no factory, no open host service) failed DCA-ADV-009/010/013/014 and DCA-STR-005. Rules select by root-namespace regexes, so the extra types do not enter their selections (verified: 237 self-tests unchanged, sample 110/110).
- `PORTING.md` corrections after the port: `Types().That()` has no `AreNotInterfaces()` in 0.13.4 (use `FollowCustomPredicate`); `SharedKernelAttribute` lives in `.Ddd.Strategic.Relationships` and must be fully qualified inside a `…SharedKernel` namespace.
- Non-generic base interfaces `IEntity`, `IAggregateRoot`, `IRepository` added to the building blocks so assignability can be tested without closing generics.

## Hexagonal, Layered, Onion

Ported by one agent; the three sets share **one fixture tree** `tests/DomainCentric.ArchRules.Tests/Fixtures/Hexagonal/{Good,Bad}` (contexts `Ordering`, `Shipping`, shared kernel, `Infrastructure.Config`). Test classes: `Rules/HexagonalRulesTests.cs`, `LayeredRulesTests.cs`, `OnionRulesTests.cs`. Framework shims live in `tests/…/Shims/AspNetCore.cs` (`Microsoft.AspNetCore.Mvc.ControllerBase`, `ApiControllerAttribute`, `Microsoft.AspNetCore.Mvc.RazorPages.PageModel`, plus `Microsoft.EntityFrameworkCore.KeylessAttribute` as ORM-attribute stand-in). `System.Transactions.TransactionScope` is the real BCL type.

- **Ported:** DCA-HEX-001…010, DCA-LAY-001…005, DCA-ONI-001…003. **n/a:** none.
- **Semantic deviations**
  - DCA-HEX-003: "controller" = name ends with `Controller` or `layout.RestControllerSuffix`, **or** derives from `FrameworkTypes.ControllerBase`/`PageModelBase`, **or** carries `ApiControllerAttribute` (Java: name only). Repository dependency = target assignable to non-generic `IRepository`.
  - DCA-HEX-006/007: event-consumer exemption is the namespace `…Adapter.Incoming.Event` (Java `..adapter.incoming.event..`).
  - DCA-HEX-009: "top level" = `!IType.IsNested`; the `package-info` exclusion has no counterpart.
  - DCA-LAY-004: `@Transactional` on methods/classes → "type has a dependency on `FrameworkTypes.TransactionScope`"; allowed in application layer and outgoing adapters (as in Java).
  - DCA-LAY-005: the building-blocks types are not part of the analysed root namespace, so the rule inspects `typeof(IOutputPort).Assembly` by reflection (namespace `DomainCentric.BuildingBlocks.Hexagonal.Ports.Out`).
  - DCA-ONI-002: hand-written loop over `IType.Dependencies`; allowed = `layout.DomainPattern` (covers the shared kernel domain), `ThirdPartyNamespacesAllowedInDomain`, building-blocks `Ddd.Tactical` and `Hexagonal.Ports.Out`. Subjects are the application's domain types only (Java also selects the marker packages themselves).
  - DCA-ONI-003: "no Spring/JPA annotations" → no attribute on a domain-model type or its members whose namespace is outside `ThirdPartyNamespacesAllowedInDomain`. Caveat: `System.ComponentModel.DataAnnotations.Schema.[Table]` etc. are under `System` and therefore *allowed* by the default list; EF Core's own attributes (`Microsoft.EntityFrameworkCore.*`) are caught.
- **Without negative fixture:** DCA-LAY-001 (diagnostic, never fails), DCA-LAY-005 (checks the published building-blocks assembly, which only contains interfaces).
- **Core gap found (not fixed, affects every dependency rule):** ArchUnitNET drops the compiler-generated async state machine (`<Method>d__N`) and does not fold its dependencies into the declaring type — a dependency that appears **only inside an `async` method body is invisible** to `Types().Should().NotDependOnAnyTypesThat()` and to `IType.Dependencies`. Fixture violations therefore sit in fields or synchronous members. Candidate fix in `DcaArchitecture`/`ArchLoader` configuration or a core helper that merges nested compiler-generated types' dependencies.

## Strategic

- Ported: `DCA-STR-001` … `DCA-STR-010` (all ten, same order). N/a: none.
- `package-info` annotations → `[BoundedContext]`/`[SharedKernel]` on the context marker class (`arch.BoundedContexts`, `arch.SharedKernelNamespace`).
- ArchUnit `..api..` / `..events..` / `..acl..` / `..adapter.incoming.openhost..` / `..adapter.outgoing.event..` → regex "segment path anywhere on segment boundaries" with PascalCase segments `Api`, `Events`, `Acl`, `Adapter.Incoming.OpenHost`, `Adapter.Outgoing.Event` (adapter/incoming/outgoing segments from `DcaLayout`, `OpenHost`/`Event` literal like in Java).
- `noClasses().that()…dependOnClassesThat().resideInAnyPackage(p…)` → `Types().That()…Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(alternation)`. `Types()` (not `Classes()`) so interfaces and records are subjects too.
- `DCA-STR-005` uses `HaveAnyAttributes(typeof(OpenHostServiceAttribute))` — the attribute may sit on classes *and* interfaces (`AttributeTargets.Class | Interface`), unlike Java where only classes were annotated.
- `DCA-STR-008` "beRecords" → hand loop over `arch.Classes` implementing `IIntegrationEvent` with `Class.IsRecord`; `record struct`s are ArchUnitNET structs and pass implicitly.
- `DCA-STR-009` name suffixes via `HaveNameMatching("(EventTranslator|ACL|AntiCorruptionLayer)$")`.
- Diagnostics `DCA-STR-001` print to `Console`; `DCA-STR-010` stays a no-op.
- No negative fixture: `DCA-STR-001` (diagnostic), `DCA-STR-010` (documentation-only).

## ContextMap

- Ported: `DCA-MAP-001` … `DCA-MAP-005`, `DCA-MAP-007` … `DCA-MAP-013` (12 rules). N/a: `DCA-MAP-006` (Spring Modulith `allowedDependencies` agreement — .NET has no module system annotation; project boundaries take that role).
- `contractPackages` (ArchUnit `..` patterns) → `ContractNamespaces` (namespace *prefixes*, matched with `DcaLayout.IsBelow`).
- Channel package `api`/`events` → namespace segment `Api`/`Events` (`Consumes.ToString()`), so `<Ctx>.Api` / `<Ctx>.Events` are the published interfaces.
- Dependency-based rules (`007`–`011`) are hand loops over `arch.Types` and `IType.Dependencies` (target namespace below the channel/contract namespace, self-references ignored) instead of fluent `noClasses().that().resideInAPackage(..).and().resideOutsideOfPackage(..)`; one violation line per offending type. Semantics identical; messages name the offending type.
- Enum/attribute wording in messages follows .NET (`Implemented`/`Planned`, `AntiCorruptionLayer`, `[Upstream("Catalog", Translation..., Consumes...)]`).
- Mermaid id normalization (`ext_` + lowercase, non-alnum runs → `_`) copied verbatim for `DCA-MAP-003`.
- Private helpers in the rule class (no core gap): `AllRootNamespaces` (via `arch.RuntimeTypes()` + `RootContextNamespace`), `TypesBelow`, `DependsOnNamespace`.
- No negative fixture: `DCA-MAP-013` (diagnostic).

## Dotnet

.NET-native set (`DCA-NET-001…005`, `Name = "dotnet"`), no Java counterpart; `NotApplicable` is empty. Decided design: ports are async-only, the domain stays synchronous.

- `DCA-NET-001` Domain layer must stay synchronous — every type in `AllDomainPatternsWithSharedKernel()` must have no ArchUnitNET dependency on `System.Threading.Tasks.Task`/`Task<T>`/`ValueTask`/`ValueTask<T>` or `System.Threading.CancellationToken` (checked on `IType.Dependencies` target names, so return types, parameters, fields and async bodies are all covered — in Debug builds).
- `DCA-NET-002` Port methods returning Task must end with Async — reflection over every interface below the root assignable to `IInputPort` or `IOutputPort`; declared public instance methods (accessors excluded): awaitable return ⇔ `Async` suffix, in both directions.
- `DCA-NET-003` Use cases must expose exactly one ExecuteAsync — concrete classes implementing `IUseCase<,>`: exactly one public `ExecuteAsync`, returning `Task<T>`, last parameter `CancellationToken`.
- `DCA-NET-004` Value objects should be records or readonly record structs — `IValue` implementations in the domain (all contexts + shared kernel domain) are records (`Class.IsRecord == true`) or value types.
- `DCA-NET-005` Identifiers should be readonly record structs — `IId` implementations anywhere below the root are value types **and** records. Record detection for structs: ArchUnitNET has no record flag for `Struct`, so the compiler-generated `PrintMembers(StringBuilder)` method is used as the tell-tale (present on `record struct`, absent on plain structs). `readonly` itself is not enforced (no reliable flag; `IsReadOnlyAttribute` would be the next step).
- Fixtures `Fixtures/Dotnet/{Good,Bad}` (single file each, namespaces spell the folder path); every rule has a Good pass and a Bad fail — `NoNegativeFixture` is empty.

## ContextMapRenderer

Port of `contextmap/ContextMapRenderer` to `DomainCentric.ArchRules.ContextMap.ContextMapRenderer` — same fluent API in PascalCase (`Of`, `IncludeExternalSystems`, `IncludePlanned`, `WithMermaid`, `WithTitle`, `Render`, `WriteTo(string)`), same markdown structure: headings, table columns, row order (contexts sorted by short name, ordinal), mermaid node/edge syntax, labels (`ACL`/`Conformist`, `api`/`events`, `implemented`/`planned`, `outbound`/`inbound`, ` / planned` suffix), `ext_<slug>` ids, partnership dedup and `—` placeholders.

- Wording deviation, deliberate: the preamble names `[BoundedContext]`/`[Upstream]`/… "context attributes" and "`api`/`events` namespaces, `[OpenHostService]`" where the Java text says `@Upstream` "package annotations" and "named interfaces". Structure and every generated data line are identical.
- Published interfaces: Java requires classes in `<ctx>.api` **and** a Spring Modulith `@NamedInterface` (falls back to class presence without Modulith). .NET has no such declaration, so type presence in `<Ctx>.Api` / `<Ctx>.Events` (segment matched case-insensitively, rendered lowercase) stands alone. `[OpenHostService]` is not evaluated by the renderer (neither is it in Java).
- `WriteTo` writes UTF-8 without BOM and creates parent directories, like `Files.writeString`.
- Fixture `Fixtures/ContextMapRender` mirrors the Java fixture (Cart/Catalog/Shipping, planned upstream, external "Carrier API", symmetric partnership); tests mirror the five Java assertions (module short names are PascalCase here: `Cart`, `Catalog`, `Shipping`).

## UseCase

- Ported: `DCA-USE-001` … `DCA-USE-011` (all 11). N/a: `DCA-USE-013` (2026-08-30; "no remote-capable port inside a @Transactional use case" — .NET marks no use case transactional, the boundary is a decorator or `ITransactionBoundary.InTransactionAsync`; `DCA-NET-006` covers the .NET side by keeping EF Core/System.Data/System.Transactions out of the application layer) and `DCA-USE-012` (2026-08-30; "publishing use cases must be transactional" guards Spring's after-commit relay, which has no .NET counterpart — after-save delivery is the outbox adapter's job).
- `DCA-USE-001`: matches interfaces named `InputPort` **or** `IInputPort`; they must reside exactly in `DomainCentric.BuildingBlocks.Hexagonal.Ports.In` (only types below the root namespace are loaded, so effectively: no application may define its own base input port).
- `DCA-USE-002/003/006/008` use `Types()` (records are classes, `record struct`s are structs — both are covered). `DCA-USE-006` exempts `IValue` implementors as in Java.
- `DCA-USE-004/005/007` ("final or records"): a `Command`/`Query`/`Result` **class** in the application layer must be a record or `sealed`; structs and interfaces are not checked (hand-written loop, `Class.IsRecord`/`IsSealed`).
- `DCA-USE-009`: "calls `save` on a `Repository`" → calls `SaveAsync` on a type assignable to `IRepository`; "calls `publishAndClearEvents` on a `DomainEventPublisher`" → `PublishAndClearEventsAsync` on `IDomainEventPublisher`. **Deviation in mechanics:** `await`ed calls are compiled into a nested async state-machine type that ArchUnitNET does not load (`MethodCallDependency`s of the use case class only show the `AsyncTaskMethodBuilder` plumbing). The rule therefore falls back to a reflection-based IL scan (`call`/`callvirt`/`newobj` targets of the runtime type and its nested types) when the model shows no call — private helper in `UseCaseRules`; a core-level "calls including nested state machines" helper would be the natural home if another rule needs it. The IL scan also keeps the rule working on Release builds, where the state machine is a struct. The Bad fixture's `CancelOrderUseCase` is `async`, so this fallback is what detects it (verified in the Debug test run).
- Rules without negative fixture: none (Bad fixture: own `IInputPort`, Command/Query/Result in the domain, mutable non-sealed Command/Query/Result, `CancelOrderResponse` in the application, `CancelOrderUseCase` saving without publishing and returning a DTO, `Order.ToDto()` in the domain).
- Fixture note: the context is named `Ordering` (not `Order`) because a namespace segment `Order` would shadow the aggregate type `Order` inside `…Order.Domain.Model`; the `[SharedKernel]` attribute is written fully qualified because the `SharedKernel` namespace segment shadows it.

## Naming

- Ported: `DCA-NAM-001`, `003`–`011` (10). N/a: `DCA-NAM-002` (`@Service` — .NET has no service stereotype; use cases are registered in the DI container by code).
- `DCA-NAM-001`: non-record classes in the application layer assignable to `IInputPort` (Java: `implement(UseCase)`; every `IUseCase<,>` is an `IInputPort`) must end with `UseCaseSuffix`. Assignability via the ArchUnitNET model with a reflection fallback (generic interface closure).
- `DCA-NAM-003`: input-port interfaces must be named `I*InputPort` — the .NET `I` prefix is an **additional** expectation; base names `InputPort`/`IInputPort`/`UseCase`/`IUseCase` are excluded as in Java. Id/title unchanged.
- `DCA-NAM-004`: interfaces containing `Repository` (except `Repository`/`IRepository`) must end with `Repository`; no `I` prefix demanded (Java parity).
- `DCA-NAM-005` (`@Controller`) → classes deriving from `FrameworkTypes.ControllerBase` **without** `[ApiController]`, or from `FrameworkTypes.PageModelBase`, must end with `Controller`. **Deviation:** Razor `PageModel`s are included (DCA names them `*PageController`; plain Razor Pages convention would be `*Model`). `DCA-NAM-006` (`@RestController`) → classes carrying `FrameworkTypes.ApiControllerAttribute` must end with `RestControllerSuffix` (layout default `Controller`, Java default `Resource`).
- `DCA-NAM-009`: technical bucket segments matched case-insensitively (`Entities`, `ValueObjects`, `Helpers`, `Util`, `Utils`) anywhere below the root namespace.
- `DCA-NAM-010`: additionally flags the `Implementation` suffix (`Manager|Helper|Util|Utils|Impl|Implementation`).
- `DCA-NAM-011`: `Root.*.Adapter.Incoming.Web` and below (segments from the layout, `Web` literal as in Java).
- Uses the shims `Microsoft.AspNetCore.Mvc.ControllerBase`, `ApiControllerAttribute`, `RazorPages.PageModel` from `tests/…/Shims/AspNetCore.cs` (created by the Hexagonal/Layered/Onion agent; not modified).
- Rules without negative fixture: none.

## Cycles

- Ported: `DCA-CYC-001` … `DCA-CYC-004` (all 4). N/a: none.
- Java `slices().matching(base + ".(*).domain.model..")` → `SliceRuleDefinition.Slices().Matching($"{Root}.(*).Domain.Model")` etc. — one slice per context for the given layer; ArchUnitNET's `Matching` includes sub-namespaces of the captured pattern (verified: the Bad fixture with `Alpha.Domain.Model ↔ Beta.Domain.Model`, `Alpha.Application.Shared ↔ Beta.Application.Shared`, `Alpha.Adapter.Incoming ↔ Beta.Adapter.Incoming`, `Alpha.Adapter.Outgoing ↔ Beta.Adapter.Outgoing` fails each rule; the Good fixture passes). Segment names come from `DcaLayout`; `Model` is literal as in Java.
- Rules without negative fixture: none.

## Tactical

- **Ported:** DCA-TAC-001 … DCA-TAC-022 (all 22, Java order, same ids/titles/rationales with .NET wording: `IAggregateRoot`, `IId`, `IValue`, `IRepository`, `IStore`, "namespace", "sealed"/"readonly", "property setter").
- **n/a:** none (`TacticalPatternRules.NotApplicable` is empty).
- **Semantic deviations / .NET readings:**
  - Assignability to the markers is decided by reflection on the runtime type (`arch.RuntimeType`), falling back to ArchUnitNET `IsAssignableTo(fullName)` for referenced-only types — ArchUnitNET does not reliably see interfaces implemented by a base class from another assembly (`AggregateRootBase<,>`).
  - "fields" (Java `getAllFields`) → instance **fields and properties**, inherited included (`Class.MembersIncludingInherited`); compiler-generated members (`<X>k__BackingField`, `EqualityContract`) are skipped. Collection element types come from the member's `GenericArguments` (any generic, not only `List/Set/Collection`).
  - DCA-TAC-004: an `IId`-typed field or property is required (Java semantics kept; `Id` by name alone is not enough). Because `IEntity<TSelf,TId>` forces `TId Id`, the negative fixture implements the non-generic `IEntity` with a `string Id`.
  - DCA-TAC-005: records and structs are exempt (Java: records); "package-private" → `internal`.
  - DCA-TAC-006/011: a setter is a property whose `Writability == Writable` (an `init` accessor is `InitOnly` and therefore not a setter) or a `Set*` method with one parameter returning `void`. 006 = public only; 011 = any visibility (Java `isSetter` had no visibility filter either).
  - DCA-TAC-009: `final class` → `sealed`; **records must be `sealed` too** (a C# record is inheritable, unlike a Java record); structs (incl. `record struct`) and abstract classes are exempt.
  - DCA-TAC-010: `readonly` fields and no `Writable` property; records/structs/enums exempt (a mutable `record struct` with `{ get; set; }` is thus not flagged — use `readonly record struct`).
  - DCA-TAC-012: record/struct/enum, else runtime type declares `Equals(object)` and `GetHashCode()` somewhere below `object`/`ValueType` (Java: `getAllMethods` minus `Object`).
  - DCA-TAC-013/016/018: the interface-name convention is `I<Aggregate>Repository` / `I<Name>Store`; the leading `I` is stripped before the aggregate name is resolved (016) and `IRepository`/`IStore` themselves are excluded like Java's `Repository`/`Store`.
  - DCA-TAC-016 keeps the Java **name-based** resolution (`I<X>Repository` → class `X` in the same context must be an `IAggregateRoot`; unresolvable → violation). The generic argument of `IRepository<TAggregate,TId>` cannot be misused because the interface constrains `TAggregate : IAggregateRoot<,>` at compile time.
  - DCA-TAC-017: return types are unwrapped recursively via `ReturnTypeInstance.GenericArguments` (`Task<Shipment?>`, `Task<IReadOnlyList<T>>`, …). Only declared (not inherited) methods, like Java `getMethods`.
  - DCA-TAC-021: forbidden names are `FindByIdAsync/SaveAsync/DeleteByIdAsync/DeleteAsync`, the non-`Async` forms and the Java camelCase names. ArchUnitNET `MethodMember.Name` carries the parameter list (`SaveAsync(System.String)`), so names are compared before the `(`.
  - DCA-TAC-022: "record" = `Class.IsRecord` or a struct; must implement `IValue`; types implementing `IFactory` are excluded as in Java.
  - Fluent-API-free: every rule is a `DcaRule.Check` loop over `arch.Types`, because the fluent `AreAssignableTo(typeof(...))` could not be trusted for markers implemented through a base class in another assembly (see first bullet). Namespaces are matched with the `DcaLayout` regexes.
- **Rules without negative fixture:** none — every rule has a Good pass and a Bad fail (`TacticalPatternRulesTests`: 45 tests, 1 shape + 22 good + 22 bad).
- **Note for the coordinator:** the shared build was red from other agents' files during this port (`AdvancedPatternRules.cs`, `UseCaseRules.cs`, `ContextMapRules.cs`); the tactical set was verified in an isolated copy with the other rule sets stubbed.

## Advanced

- **Ported (18/18):** `DCA-ADV-001` … `DCA-ADV-018`, same ids, order, titles and rationales (wording adapted: "package" → "namespace", `DomainEvent` → `IDomainEvent`, `@IntegrationEventType` → `[IntegrationEventType]`, "final" → "sealed"/"readonly", "Spring annotations" → "framework attributes").
- **n/a:** none. The four "must not have Spring annotations" rules (`004`, `011`, `015`, `018`) are ported with the .NET reading from `PORTING.md`: the type carries no attribute whose namespace is outside `DcaLayout.ThirdPartyNamespacesAllowedInDomain` / the building blocks / the domain layer itself (read via reflection `GetCustomAttributesData()`, so compiler-generated `System.Runtime.CompilerServices.*` attributes are allowed). Bad fixtures use stand-in stereotype attributes under `…Bad.Infrastructure.Stereotypes` (`[Component]`, `[Service]`).
- **Semantic deviations:**
  - `001` "be records": C# `record` (`Class.IsRecord`) **or** a struct (`record struct`s are `Struct`s in ArchUnitNET); `003` is only checked for non-record classes (must be `sealed`); enums are not classes in ArchUnitNET, so the Java `areNotEnums()` guard has no counterpart.
  - `006`/`007` "version field": any field on the type or its ancestors whose logical name is `version` (case-insensitive); an auto-property backing field `<Version>k__BackingField` counts as `Version`, so `record X(…, int Version)` is caught like the Java record component.
  - `008` "timestamp field": a field (incl. auto-property backing field) of type `DateTimeOffset` or `DateTime` (Java `Instant`/`LocalDateTime`/`ZonedDateTime`). A computed `OccurredOn => …` property has no field and violates — mirrors the Java fixture `OrderArchived`.
  - `012`/`016` "only final fields": every field on the type or its ancestors must be `readonly` (`FieldInfo.IsInitOnly`) or `const`; a settable auto-property therefore violates via its writable backing field, a get-only/init-only property passes. Field inspection uses reflection (`arch.RuntimeType`) instead of the ArchUnitNET member model.
  - `009` uses the pattern `^.*\.<DomainSegment>\.Service(\..*)?$` (Java `..domain.service..`).
  - `IIntegrationEvent` does not extend `IDomainEvent` in the building blocks (as in Java), so `002`/`008` do not apply to integration events and `007`'s "not an integration event" guard is only defensive.
  - ArchUnitNET 0.13.4 has no `AreNotInterfaces()` on `Types().That()` (contrary to `PORTING.md`); replaced by `FollowCustomPredicate(t => t is not Interface, …)`.
- **Rules without negative fixture:** none.
- **Gaps reported:** none in the core; no shims needed (no framework types involved).

## Cycles (follow-up, 2026-08-30)

- ArchUnitNET `SliceRuleDefinition.Slices().Matching("Root.(*).Domain.Model")` does **not** restrict the slice to the trailing segments: every type below `Root.<ctx>` is assigned to a slice named by its full sub-namespace, so `Product.Domain.Model` and `Product.Domain.Event` became two slices and their (legitimate) mutual references a "cycle" — reported identically by all four `DCA-CYC` rules. The dca-dotnet fixtures only had one namespace per layer and never noticed; `dca-ecommerce-sample-dotnet` did on its first run.
- Replaced by a hand-rolled check in `CycleRules.CheckSlices`: regex `^Root\.([^.]+)\.<Layer>(\..*)?$` assigns types to a slice per context, edges are `IType.Dependencies` between different slices, elementary cycles are enumerated (smallest node first, each once) and reported with their member dependencies. `DcaRule.Of` → `DcaRule.Check`. 237 self-tests unchanged, sample 111/111.

## .NET-only rules (follow-up, 2026-08-30)

- `DCA-NET-006` added: types in a context's `Application` namespace must not depend on `Microsoft.EntityFrameworkCore.*`, `System.Transactions.*`, `System.Data.*`, `Dapper.*`, `NHibernate.*`, `MongoDB.Driver.*`. The transaction boundary is a decorator around `IUseCase` or the `ITransactionBoundary` port; the adapter behind it is the only place that knows the framework. Fixtures: Good `ShipOrderUseCase` (`ITransactionBoundary.InTransactionAsync`, remote `ICarrierPort` outside), Bad `ShipOrderUseCase` (`TransactionScope` in the use case).
- `DCA-LAY-005` skips compiler-generated nested types in `Ports.Out` (found while `ITransactionBoundary` still lived there as `IUnitOfWork`: its default interface method produces a closure class and an async state machine). Same day it was renamed and moved to `Application.Transactions` — a transaction boundary is execution semantics, not an output port.

## Context discovery, module discovery, structural isolation (2026-09-03, planning WP-20 + WP-21)

- **Same one-segment bug as Java, same fix.** `DcaLayout.Segment` (`[^.]+`) in the wildcard patterns and
  the first-segment cut in `RootContextNamespace` meant a grouped (`Root.Sales.Order`), nested or flat
  layout matched no layer rule and passed for lack of subjects. `RootContextNamespace` now walks up to the
  nearest declared ancestor (memoised per namespace); `ContextName` is the namespace relative to the root
  (`Sales.Order`), used by `ContextMapRules`, `ContextMapRenderer` (the two `ShortName` copies are gone)
  and rule messages; `SimpleContextName` is `[Obsolete]`. All eight sample contexts sit at depth 1, so the
  identifier change is a no-op there — measured: `dotnet test` 127 + 29 + 113 green, `docs/context-map.md`
  byte-identical.
- **Module roots** (`ModuleRoots()`, shortest prefix owning a layer) drive every layer, hexagonal, onion,
  naming, tactical, use-case and .NET-only rule via `All*Patterns()`; declared contexts drive only the
  context-map rules and `DCA-STR-001/002`. ArchUnitNET's `ResideInNamespaceMatching` takes one regex, so
  the arrays are combined with `DcaLayout.AnyOf` (empty → `(?!)`, never an empty string that matches all).
  Rules whose helpers took only the layout (`AdvancedPatternRules.InDomain`, `IsAllowedAttribute`) take the
  architecture now, as `UseCaseRules.immutableApplicationModels` did in Java.
- **Cycle rules** slice by `ModuleRootOf(namespace)` instead of the one-segment capture group; the
  hand-rolled slicing stays (ArchUnitNET's `Slices().Matching` reports intra-context pairs as cycles).
- **Isolation is structural** (WP-21, same shape as Java): `DCA-STR-003/004/006` and `DCA-HEX-007` loop
  over `IsolatedModuleRoots()` on both sides; `DCA-STR-006`'s forbidden set is "foreign module minus its
  `Api`/`Events`", expressed as one regex with a negative lookahead (`(?!published)(?:foreign)`) because
  the fluent API has no "except". The four collect their per-module violations via `DcaRule.EvaluateAll`.
  `DCA-STR-005` accepts `Api` or any namespace under `Adapter.Incoming`. `Api`/`Events` come from
  `DcaLayout.ApiSegment`/`EventsSegment` everywhere; the renderer lower-cases them for the map labels so the
  generated document is unchanged. **No `DCA-LAY-006`** — deleted on the Java side before release.
- **allowEmptyShould asymmetry recorded:** .NET passes every empty selection (`DcaRule.Of`), so nothing
  had to change for transaction-script contexts; the `TransactionScript` fixture runs the whole catalog green.
- Fixtures `Fixtures/Layout/*` (grouped, flat, nested, grouped module, empty context, transaction script,
  grouped cycle, isolation) and tests `ContextDiscoveryTests`, `StructuralIsolationTests` — 300 self-tests.
  `ContextDiscoveryTests.NoRuleUsesTheWildcardPatterns` greps the rule sources so the wildcard cannot creep back.

## Features within a bounded context (2026-09-06, planning WP-23)

- `DCA-USE-014` and `DCA-CYC-005` ported the day Java added them, same ids/titles/rationales (namespace
  wording instead of package wording, as everywhere else). `USE-014` walks `ModuleRoots()`, takes the
  namespace relative to `<module>.Application`, skips `Application.Shared` and nested types (`IType.IsNested`
  — which also drops the compiler's async state machines) and collects all offenders into one
  `DcaRuleViolationException`. `CYC-005` reuses the hand-rolled slice graph of `CYC-001..004`: `CheckSlices`
  now takes a namespace→slice function; the layer rules pass "module root when below the layer", the new rule
  passes "`<module>.Application.<first child>`, null for `Shared`". Neither rule is `NotApplicable` — nothing
  here depends on a Java-only framework.
- Fixtures `Fixtures/Features/{CompatFixture,DepthFixtures,SliceFixtures}.cs` mirror the Java
  `fixtures.features.{compat,depth,slices}` packages one to one (`Application.Ordering.PlaceOrder`,
  `Application.Shared`, `Adapter.Incoming.Web.Ordering`); the `UseCase/Bad` and `Cycles/Bad` fixtures gained
  a mixed-depth use case and a `Quote ↔ Booking` cycle so the set-wide negative tests keep one failing case
  per rule. 319 self-tests. Rule count 112 → 114.

## Shaping the result (2026-09-06, planning WP-24)

- `DCA-USE-015` and `DCA-HEX-012` ported the day Java added them, same ids/titles/rationales (namespace
  wording). `USE-015` does not rely on the ArchUnitNET member model: ArchUnitNET exposes record positional
  properties, but not generic arguments of member types, so the rule resolves each `*Result` (non-`IValue`,
  application namespace) to its runtime `Type` via `DcaArchitecture.RuntimeType` and walks public properties
  and fields by reflection — `IsGenericType` → `GetGenericArguments()` (covers `IReadOnlyList<T>`,
  `IReadOnlyDictionary<K,V>` and `Nullable<T>` alike), records recognised by their synthesized `<Clone>$`
  method — record classes via `<Clone>$`, record structs via `PrintMembers` — parts being records anywhere in an
  application namespace (nested, next to the result, or in `Application.Shared`), arrays via their element type,
  generic parts through their type arguments and their own members, a visited set against
  self-referencing parts, `EqualityContract` skipped. Identity is `typeof(IAggregateRoot).IsAssignableFrom`
  / `typeof(IEntity).IsAssignableFrom`; each hit is reported with the member path. `HEX-012` mirrors
  `HEX-011`: incoming-adapter classes whose `Dependencies` target an `IDomainService`, plus the runtime
  type's constructor parameter types, since ArchUnitNET attributes async bodies to the state machine.
  Neither rule is `NotApplicable`.
- Fixtures: `UseCase/Good` gained `ListOrders` (a result of `IReadOnlyList<OrderSummary : IValue>`, a nested
  `OrderLine` part, a same-namespace `OrderTotals` part and a `Money?`), `UseCase/Bad` a `ListOrdersResult`
  with `IReadOnlyList<Order>`, a nested part holding `Order`, a sibling `LineView(OrderLine : IEntity)`, an
  `Application.Shared` part `OrderPart(OrderLine)`, an `Order[]`, a `record struct LinePart(Order)` and a generic
  `Boxed<int>(T Value, Order Extra)`;
  `Hexagonal/{Good,Bad}` gained `Domain/Service/PricingPolicy : IDomainService`, used by the Good use case,
  injected into the Bad `OrderController` and called statically by the Bad `OrderQuoteController`; the
  outgoing `InMemoryOrderRepository` keeps depending on domain types and stays green. 326 self-tests.
  Rule count 114 → 116.

## Review follow-up (2026-09-06)

The package review of `dca-java` (`notes/dca-java-review-2026-09-06.md` in the meta-repository) found ten
enforcement defects; four of them existed here too and are fixed the same day with the same semantics:
`DCA-MAP-001` inspects `NamespacesBelowRoot()` (nested marker classes included); `DCA-USE-015` walks public
instance members without `DeclaredOnly` and keeps the records on the current path instead of a global
visited set; `DCA-NAM-011` derives the web-adapter namespaces from `ModuleRoots()`; `.ignore` values are
one regular expression with indexed keys for several. Widened alongside Java: infrastructure means the
global namespace plus every isolated module's `Infrastructure` (`InfrastructureNamespaces()`,
`AllInfrastructurePatterns()`), used by `LAY-002/-003`, `HEX-004/-005`. Not needed here: the context-map
rules already collected every violation, normalised with the invariant culture and matched
`Equals(object)`/`GetHashCode()` exactly; `USE-012/-013` remain n/a. Fixtures: `ContextMap/Nested`,
`Layout/Infra`, view models in `Layout/{Grouped,Flat}`, `BaseListing`/`ArchivedOrdersResult` and a second
`LineView` in `UseCase/Bad`. Self-tests 326 → 336. Rule count unchanged (116).

## Recheck follow-up (2026-09-07)

The recheck of the Java follow-up (`notes/dca-java-recheck-2026-09-07.md` in the meta-repository) found that
`DCA-USE-009` here still checked class-wide while its rationale already promised per-method reasoning. Ported
the same day: `Rules/IntraClassCalls.cs` builds the directed call graph of a use case from the runtime type's
IL (the `IlCalls` scanner moved there from `UseCaseRules`); the compiler's async state machines
(`[AsyncStateMachine]` → every method of the nested type) and lambda closures (`ldftn`/`newobj` operands)
are units of the graph, joined to the method that declares them, and are reported under that method's name
(`ExecuteQuietlyAsync (via PersistAsync)`). `DCA-USE-009` judges every entry path to a saving unit, exactly
like the Java rule; `USE-012/-013` remain n/a. The second recheck the same day tightened the entry-point
definition on both sides: any non-private, non-compiler-generated unit is an entry point, called internally
or not (`Fixtures/Transactions/…/DirectEntry`, mirroring Java's `directentry`). Fixtures `Fixtures/Transactions` mirror
`fixtures.transactions` one to one (shared helper, split helpers, multi-step, shared save helper, recursion,
mutual recursion, save without publish). Parity of the other two recheck findings: `DCA-USE-015` already
resolved generic bases (reflection substitutes type arguments — `GenericResults.cs` fixtures added, green
before any change); `DCA-TAC-003` already rejected `IReadOnlyList<Category>` inside `Category`, but missed
`Category[]` and generic-base members — `DataMembers` now adds reflection-derived element types (arrays,
generic arguments, bound type parameters) next to the ArchUnitNET generic arguments. Self-tests 336 → 345.
Rule count unchanged (116).

## Structural parity (WP-26 Part A, 2026-09-08)

Writing the `Selecting`/`Checking` texts (WP-25) showed twelve rules still selecting via `layout.*Pattern`
(`Root.[^.]+.X`, direct children of the root only) although WP-20 had made discovery depth-independent:
`TAC-001/009/013/014/015/019/020/022`, `NAM-007/008/010`, `USE-008`. All now go through `arch.All*Patterns()`;
`ONI-003` and `TAC-001/009` dropped the extra `SharedKernel.Domain` scope (the shared kernel is a module root).
`STR-002` moved from per-context `Evaluate` (first failure wins) to `EvaluateAll`. Pinned by
`ContextDiscoveryTests.Undeclared.IsGovernedByTheTacticalAndNamingRulesAtDepthTwo` (a module two segments deep,
one offender per selection shape). The class remark in `DcaLayout.cs` - "The rules do not use them" - is true
again. Remaining Java↔.NET differences are semantic and listed in `planning/WP-26-dotnet-rule-parity.md` Part B.

## Semantic parity (WP-26 Part B, 2026-09-08)

Decided per row, .NET changed where the porting had drifted: `TAC-022` (interfaces selected), `ONI-002` (building
blocks: only `Ddd.Tactical` + `Ports.Out`), `NAM-001` (`IUseCase<,>` implementors only), `USE-015` (all
visibilities, no nested classes), `STR-007` (no interfaces), `STR-008` (`arch.Types` minus interfaces, so record
structs count). Kept as deliberate .NET readings, documented in the catalog: fields **and** properties in
`TAC-002..011`; `TAC-009` selects records and demands `sealed`; the attribute allow-list in `ONI-003`/`ADV-004/011/
015/018` (no stereotype attributes to name in .NET); `HEX-003` selects controllers by base class and
`[ApiController]` too; `STR-005` keeps `Types()` - the attribute allows interfaces and ArchUnit's `classes()`
includes interfaces as well, so this was never an asymmetry. Java moved for `ADV-012/016` (inherited fields) and
`NAM-010` (`Implementation`).

