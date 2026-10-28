# Changelog — DomainCentric.ArchRules (+ DomainCentric.ArchRules.Xunit)

All notable changes to these packages. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning: SemVer.

## [Unreleased]

### Added

**`DCA-HEX-017` (experimental): event listeners that drive an input port must run after the commit
in their own transaction** — the same id as in the Java library. A class assignable to a type of the
new `EventListenerTypes` role and to none of `TransactionalEventListenerTypes` must not call an input
port from any of its methods. Both roles are empty in every preset, because the platform has no
standard listener type; a project names its own listener interfaces, or its messaging library's, with
a with-expression, and the rule then selects. The Java rule reads the listener's annotations instead;
both read the listener's kind, not whether an outbox backs it.

## [0.9.0] - 2026-10-15

### Removed

**`DCA-HEX-015` is retired.** The experimental rule "Incoming adapters must not keep state for a
later request in one instance" checked an operational requirement — several instances behind a load
balancer without sticky sessions — that a project chooses, not architecture every application
shares. The id joins the retirement list (`DcaRules.Retired()`, `rules.json` `retired`, since
0.9.0, no replacement) and is never reused. A project that names it in `Excluding(…)`,
`dca.rules.off` or a severity entry keeps loading and gets one retirement notice in the report
(`RetiredReferences`); `OnlyIds("DCA-HEX-015")` or `dca.rules.ids=DCA-HEX-015` fails with the
reason, as for every retired id. Where an application does run that way, the guidance stays in the
guide and the catalog. `HexagonalRules.IncomingAdaptersKeepNoInstanceLocalState()` goes with it —
retired ids keep no public factory.

### Deprecated

**`FrameworkTypes.InstanceLocalStateTypes` reads nothing.** `DCA-HEX-015` was its only reader. The
property stays `[Obsolete]` so a with-expression written against 0.8.0 still compiles: it is always
empty and an assignment is ignored. `AspNetCore()` no longer fills it.

### Changed

**`DCA-ERR-007` says what to do with a code held in a string.** The experimental diagnostic keeps
listing a plain string field — the type does not tell a code from display text, and no name or
shape does so reliably for every domain — and its `checks` text now names the remedy: a typed value
for the code. A code already held in a value, an id or an enum passes, as before; new fixtures pin
both cases. The same sentence in both libraries.

**`DCA-HEX-014` reads the HTTP methods of a general verb attribute.** An entry of
`GetMappingAttributeTypes` may now name a general verb attribute with the property value that makes it
GET, `<attribute>#<property>=<value>`; it matches when that property of the attribute holds the
value — a string or a member of a sequence, ignoring case. `AspNetCore()` adds
`Microsoft.AspNetCore.Mvc.AcceptVerbsAttribute#HttpMethods=GET`, so `[AcceptVerbs("GET")]` is
selected like `[HttpGet]`. A malformed attribute entry is refused. The Java twin reads
`RequestMapping#method=GET` the same way.

**`DCA-HEX-014` counts an action that names no HTTP method as a GET handler.** Such an action answers
every method, GET included, so a command behind it changes state on GET. A `GetMappingAttributeTypes`
entry without a value, `<attribute>#<property>=`, matches a public instance method of a class recognised
as a controller — not a page model, which routes by handler name — none of whose attributes of that type
holds a value; carrying none counts. `AspNetCore()` adds
`Microsoft.AspNetCore.Mvc.Routing.IActionHttpMethodProvider#HttpMethods=`, so an action with `[Route]`
alone or reached by conventional routing is selected, one with `[HttpPost]` or `[AcceptVerbs("POST")]`
is not. The new role `NonActionAttributeTypes` (`AspNetCore()`: `[NonAction]`) takes a public method
out. The rule stays experimental. The Java twin selects `@RequestMapping` without a method the same way.

**`DCA-MAP-014` reads an entry as the Java twin does.** An entry that names an edge of a `Planned`
`[Upstream]` declaration is announced, not stale, and is skipped; an entry whose module is neither a
bounded context nor a module root of the scan is skipped; `module :: *` means the whole module; a
module root is named by its last namespace segment. Before, all three were reported. Without a
configured module-declaration attribute the rule still selects nothing — the `selects` text now says
why no fallback exists: .NET draws module boundaries with projects, and the compiler drops a project
reference no code uses, so the compiled assemblies carry no stale edge to compare; the `[Upstream]`
declarations are `DCA-MAP-007`'s business.

## [0.8.0] - 2026-10-14

### Added

**Experimental rules.** A rule has a maturity beside its kind: `DcaRuleMaturity.Released` or
`DcaRuleMaturity.Experimental` (`IDcaRule.Maturity`), declared where the rule is defined with
`DcaRule.Of(...).Experimental()`. An experimental rule keeps its final id and its set, so releasing it
removes that one call. `DcaRuleSelection.All()` keeps meaning every released rule; a project opts in
with `WithExperimental()` — which composes with scope, severity and ignore settings, and survives
`MergedWith` from either side — or with `dca.rules.experimental = true` in `dca-archunit.properties`
(`true` or `false`, anything else fails). `DcaRules.AllExcept` and `DcaRules.Only` return released
rules only; `DcaRules.All` stays the whole catalog. `DcaArchitectureTest` reports experimental rules
as a group of their own, `experimental (n) / <id>`, after the rule sets — marked `[NOT OPTED IN]`
and not run when the file does not opt in — and the summary line always reads
`<n> enforced, <n> informational, <n> experimental`. `rules.json` carries `"maturity"` on every rule,
right after `"status"`.

**The first experimental rules** (WP-109) — same ids as the Java library, all `experimental`, so a suite
without the opt-in runs exactly as before:

- `DCA-TAC-024` (tactical) — a value object declares no neutral factory `Of` beside another public static
  factory that returns it and takes the same parameter types; two named origin factories with equal
  parameters (`FromInput`, `FromStored`) pass.
- `DCA-NAM-012` (naming, informational) — lists an input port or use case whose name holds a trigger word
  (`On`, `After`, `When`, `Upon`, `Before` followed by an upper-case letter).
- `DCA-NAM-013` (naming) — a use case, an input port and a public method of a module's `Api` namespace are
  not named `Set…` or `Update…` followed by the field.
- `DCA-USE-018` (usecase) — in a module grouped into features, an incoming adapter class drives the
  commands of one feature only: input ports whose input ends in `Command` fall into one feature, while a
  port whose input ends in `Query` is not counted (reading a neighbouring feature's query is allowed).
- `DCA-USE-020` (usecase) — no type of the application layer parses raw input into an id with
  `System.Guid.Parse`, `TryParse`, `ParseExact` or `TryParseExact`; the adapter or the id's value type parses.
- `DCA-USE-019` (usecase) — a use case whose input ends in `Query` calls no repository or store write and no
  event publisher.
- `DCA-HEX-014` (hexagonal) — a GET handler drives no input port that takes a `*Command`.
- `DCA-HEX-015` (hexagonal) — an incoming adapter uses no API that keeps state for a later request in one
  instance.
- `DCA-MAP-014` (contextmap) — every allowed-dependency entry of a module declaration is backed by a code
  dependency of the context on that module (or its named interface). Reads the same
  `ModuleDeclarationAttributeTypes` role as `DCA-MAP-006` and selects nothing where none is configured —
  the default, since no preset names one.
- `DCA-HEX-016` (hexagonal, informational) — lists adapter methods that turn an enum of the domain or
  application layer into its member name, as the IL shows it: `ToString` (constrained or on the boxed value),
  a boxed value passed into string concatenation or formatting, an interpolation hole, `Enum.GetName`.
- `DCA-ERR-007` (errors, informational) — lists fields of domain and use-case exceptions that hold a plain
  string or an aggregate or entity instead of ids and values.

Three new `FrameworkTypes` roles carry the framework names: `GetMappingAttributeTypes` and
`GetHandlerNamePrefixes` (`DCA-HEX-014`; `AspNetCore()`: `[HttpGet]`, the `OnGet` handler prefix) and
`InstanceLocalStateTypes` (`DCA-HEX-015`; `AspNetCore()`: `ISession`, `ITempDataDictionary`, `[TempData]`).
All three are empty in `None()`.

## [0.7.0] - 2026-10-12

**Migration from 0.6.0.** Three new rules, all `error`, can turn a suite red that was green: an
outgoing adapter that depends on an input port, an aggregate root with a public constructor, and an
interface in a module's events namespace that does not extend the integration-event role.

### Changed — breaking

**Outgoing adapters do not drive the application.** `DCA-HEX-013`, same id and texts as
`dca-archunit`: a class in `<module>.Adapter.Outgoing` depends on no type assignable to
`IInputPort` — neither an input port interface nor a use case class. An adapter that calls an input
port is an incoming adapter filed on the wrong side. Constructor parameters are read from the runtime
type as well, as in `DCA-HEX-012`, so a port injected into an all-async adapter is still found.

**An aggregate root names the ways it comes into being.** `DCA-TAC-023`: a concrete, non-record
class assignable to `IAggregateRoot` declares no public instance constructor; creation and
reconstitution go through static factory methods (`Create(...)`, `Reconstitute(...)`). Internal,
protected and private protected constructors pass. Records are not selected — a positional record's
primary constructor is public by the language — and neither are structs or abstract bases.

**An interface in the events namespace is an integration event.** `DCA-STR-013`: every top-level
interface in `<module>.Events` and below, the shared kernel's included, is assignable to
`IIntegrationEvent`, so a consumer listening to its own contract interface still reads the event's
identity and occurrence time. Nested interfaces are not selected.

## [0.6.0] - 2026-09-22

**Migration from 0.5.0.** Two new rules, `DCA-ADV-019` and `DCA-ADV-020`, can turn a suite red that
was green — a model type that reaches for a domain service, and a domain service whose operation
takes no aggregate. Both are `error`. Anyone who builds their own informational rule has one
signature to change: the factory takes a `Func` where it took an `Action`. The retired
`DCA-MAP-003` loses its last public factory. Everything else is a fix or a rendering change.

### Changed — breaking

**A domain service works on the model, and the model does not reach for it.** The .NET twins of
`DCA-ADV-019` and `DCA-ADV-020`, same ids and same texts as `dca-archunit`: a model type neither
calls nor constructs a domain service, and every public operation of a domain service takes at least
one aggregate or entity. One reading differs and says so in the `checks` text — Java reports every
call site, .NET reports each (model type, domain service) pair, because ArchUnitNET's dependency
model carries the target type rather than the instruction.

**A diagnostic reports into its outcome, not to the console.** `DCA-LAY-001`, `DCA-STR-001`,
`DCA-MAP-013` and `DCA-ERR-006` wrote their findings with `Console.WriteLine`, where no report and no
pipeline stage sees them. An informational rule returns what it observed through
`IDcaRule.Observe(architecture)`, the execution wrapper puts the lines into the outcome with the rule
id, and the rule passes because it asserts nothing. One reading differs and says so in the carrier:
xUnit v2 cannot turn a running test into a skipped one, so `DcaArchitectureTest` writes the lines to
the test output the way it already writes warnings, while the Java twin aborts the test. The outcome
is identical on both sides — which is what a report or a pipeline stage reads. Breaking for anyone
building their own informational rule: the factory takes a
`Func<DcaArchitecture, IReadOnlyList<string>>` instead of an `Action`.

**The last public factory for a retired id is gone.** `DCA-MAP-003` was retired in 0.4.0, but
`ContextMapRules.ExternalSystemNamesDistinctAfterNormalization()` was still public and still produced
that id — the same defect the `DCA-ADV-003` and `DCA-TAC-022` factories were removed for, in the one
place that was missed. Found by the cross-checkout rule-id check.

### Fixed

**`DCA-STR-010` says that it is a diagnostic.** It asserts nothing, but its title read like an
enforced rule while the other four informational ids carry the `Diagnostic:` prefix. A test holds the
convention in both directions now, as in `dca-archunit`.

### Changed

**The rendered context map is sorted.** `ContextMapRenderer` iterated the `[Upstream]` and
`[ExternalUpstream]` attributes in declaration order, so two code bases that declare the same
relationships in a different order rendered two documents that could not be compared line by line.
Declarations are now sorted by target context, first channel and translation; external systems by
name, interaction and translation. The namespaces were already sorted. A project that regenerates
its map after upgrading will see the rows move once — commit the regenerated file.

## [0.5.0] - 2026-09-21

**Every name a rule matches on is configurable.** `WithAggregateRootSuffix`,
`WithRepositorySuffix`, `WithStoreSuffix`, `WithFactorySuffix` and `WithSpecificationSuffix` join
the use-case and controller suffixes; the same six rules as in Java follow the project's names.
Defaults unchanged.

**`DCA-USE-009` exempts no more than the Java twin does.** Two gaps: the walk's stop condition was
hard-wired to `System.` and `DomainCentric.BuildingBlocks.`, so with a project's own vocabulary it
stopped nowhere and the exemption was unreachable; and a registration from outside the aggregate's
hierarchy was not looked for. The stop condition is derived from the configured marker roles now, and
the external scan is here. One .NET difference is written into the `checks` text: the library's own
`RegisterEvent` is protected, so only a nested type can reach it — and a nested type already belongs
to the enclosing type's call graph. The scan therefore matters for a vocabulary whose registration is
public, and a fixture proves it.

**`DCA-ADV-009` is retired; `DCA-ADV-010` keeps the layer** — see the Java entry.
`WithDomainServiceSegment` goes with it.

**`DCA-ADV-008` follows a configurable list of timestamp types**, `WithTimestampTypes(...)` — see
the Java entry. The default keeps `System.DateTimeOffset` and `System.DateTime`.

**`DCA-STR-009` is retired** — see the Java entry. No replacement id.

**The retirement reason of `DCA-TAC-022` is corrected** — see the Java entry.

**`DCA-NET-001` and `DCA-NET-006` say so when Java has no twin.** Both make a statement about
architecture rather than about C#: the domain layer stays synchronous, and the application layer
names no persistence or transaction API. Java checks neither — `DCA-LAY-003` forbids the
infrastructure packages, not the persistence API itself. The ids stay where they are for this
release; both `checks` texts now state the gap in one sentence, so a reader does not infer a
constraint that holds on one stack only.

**`DCA-ADV-013` and `DCA-ADV-017` are titled after what they check** — see the Java entry.

**`DCA-TAC-015` names its overlap with `DCA-HEX-008`.**

**The README has a "smallest DCA" and an "own vocabulary" section**, and the building-blocks README's
language-mapping table gained the rows it was missing: `DomainException`, `UseCaseException` and
`ISpecification<T>` — the first two are the base types the `errors` set of this release rests on.

**`DCA-TAC-007` and `DCA-TAC-008` inspect instance state only, as Java does and as all six `checks`
texts have always said.** A static field is not aggregate state — the project decided that for
`DCA-TAC-003`, and these two contradicted the decision in one language. This can turn a red build
green, never the other way round.

**`DCA-TAC-016` reads the aggregate the repository binds, and the name must name it.** See the Java
entry; `ICategoryRepository : IRepository<Order, OrderId>` is now reported, a generic intermediate
port is skipped.

**`Specification` is a role, not only a suffix.** `DcaMarkers` gains `Specification`; `DCA-ADV-017`
and `DCA-ADV-018` select through the marker or the name, resolved through the runtime type so the
generic arity of `ISpecification<T>` does not hide it.

**`DCA-NET-003` checks the contract, not a method name.** It required an operation named
`ExecuteAsync` to implement `IUseCase<TIn,TOut>` — a name reservation no source supports, and a use
case whose own port named its operation anything else was reported by no rule at all. What remains,
and what the rule is now titled and argued for, is the shape of the *configured* use-case contract:
awaitable, and taking a `CancellationToken`. With the library's own vocabulary the compiler already
enforces that, so the rule speaks where a project points the `useCase` role at a contract of its own.
Renamed to `UseCaseContractIsAsynchronousAndCancellable()`.

**`DCA-USE-013` and `DCA-MAP-006` are carried here, under the same ids.** Both were listed as not
applicable. Java carries `MAP-006` and lets it select nothing until a module-declaration annotation is
configured, so "inert until configured" is a state the library already accepts for a shared id. Both
now read a role that no .NET preset fills — `TransactionalAttribute` and the new
`ModuleDeclarationAttributeTypes` — and say so in their `checks` texts. Nothing changes for a project
that configures neither. `DCA-NAM-002` is the one remaining n/a, and the only one whose reason lies in
the platform.

**`DCA-LAY-004` sees a declarative boundary.** It read only the transaction API and the manager, so a
project with its own transaction attribute could put it on a domain type and pass. The third selection
the Java twin has always had is here now, reading `FrameworkTypes.TransactionalAttribute` — empty in
both presets, so inert without configuration.

**`DCA-ERR-004` has the same contract as in Java, and stops skipping silently.** Three empty roles —
web controller, REST controller, event listener — join the four it read, so the id means the same
thing on both stacks and a project whose framework has such an attribute can name it. A type whose
runtime reflection is unavailable is counted and named on standard output instead of passing unseen.

**Dead public API is gone while the version is unpublished.** Three unused namespace constants and the
two public factory methods for the retired `DCA-ADV-003` and `DCA-TAC-022` are removed; a new test
asserts that no rule set offers a retired id.

**`DCA-NAM-003`, `DCA-ONI-003` and `DCA-HEX-009` say what they presume** — the `I` prefix as a platform
convention, the container role as empty in both presets (which is why the half of `ONI-003` that does
the most work in Java selects nothing here), and the common port root `DCA-HEX-009` needs.

**One more hard-wired segment.** `DCA-USE-014`'s depth check still read a literal `Shared`; it reads
the configured segment now.

**A run says which preset and which vocabulary it resolved.** `DcaArchitectureTest` gained a
`LayoutDiagnostics` fact that prints `framework annotations: aspnetcore (default)` and
`building block markers: dca (library default)` — the same two lines, in the same wording, the Java
carrier has printed since 0.4.0. New: `DcaLayout.FrameworkTypesReport()`. A mistyped role or a wrong
root namespace is no longer a green suite with nothing to read. The README now also states, in the
paragraph that exists for exactly this, that .NET has neither framework detection nor a preset SPI.

**The advanced rules no longer change their verdict when a type cannot be loaded.**
`DCA-ADV-006`, `-007`, `-008`, `-012` and `-016` read fields through the CLR only; for a type
ArchUnitNET models but the loader cannot resolve, four of them passed silently and `DCA-ADV-008`
reported a violation that was not there. They now fall back to the ArchUnitNET member model, and the
five `checks` texts say what that model does not carry — the backing fields of auto-properties — and
that a field whose writability neither model states is not reported.

**`DCA-USE-012` says what to change.** The message named the missing property, not which of the two
accepted forms to add — and with no transactional attribute configured, which is the .NET default,
only one of them was available. It now names the configured attribute (or "none configured") and
`ITransactionBoundary.InTransactionAsync(...)`, mirroring the Java wording, and the header is the
rule's own title. Its `checks` text now states that the declarative path is empty by default here
while the Java presets fill it.

**`DCA-ERR-005` no longer rejects Ubiquitous-Language names.** The word list matched `Status` and
`Response` as substrings, so `OrderStatusInvalidException` failed the rule that exists to protect the
language. It now matches `Http`, `StatusCode`, `ResponseStatus` and `ResponseEntity`, in both
languages.

**`DCA-TAC-021` matches the same six write names in both languages**, compared case-insensitively
instead of through a nine-entry list that spelled the Java names out.

**`DCA-NET-004` says what it checks.** The rule is "Struct value objects must be readonly", but its
rationale argued for records, its factory was called `ValueObjectsShouldBeRecords()` and the set's
comment said "records for values". A reader who followed the rationale converted classes to records
for no reason — `DCA-TAC-012` accepts any immutable type with value equality. Renamed to
`StructValueObjectsMustBeReadonly()`; the rationale now argues from the copy semantics of a struct.

**Four `DCA-USE` titles say what the rule checks** — `DCA-USE-002`, `-003`, `-006` and `-008` named
the suffix as a requirement when it is the selection. Titles are not ids; nothing breaks.

**`DCA-USE-015`'s `checks` text matches the walk**, which binds public and non-public members, and
says that a type the loader cannot resolve is not walked.

**The web adapter and shared segments are configurable.** `WithWebSegment(...)` and
`WithSharedSegment(...)` join the other segments; `Web` was hard-wired in `DCA-NAM-011` and `Shared`
in the shared-output-port patterns and the cycle slicing. Defaults unchanged, and the shared segment
stays reserved against the operation containers whatever it is called.

**`DCA-STR-008` cites the right reason** — a published contract, not event sourcing, which DCA
prescribes nowhere.

**`dca.framework` works here too.** The Java library lets `dca-archunit.properties` select a framework
preset by name; .NET only accepted one in code. `dca.framework=aspnetcore|none` is now read from the
same file, an unknown name fails and lists the known ones, and a layout that sets its framework types
in code always wins over the file. New: `FrameworkTypes.Preset(name)`, `FrameworkTypes.PresetNames`,
`DcaLayout.WithFrameworkPreset(name)`, `DcaLayout.FrameworkTypesSource`,
`DcaLayout.ApplyConfiguredFramework(layout, properties)` and `DcaRuleSelection.PropertiesFrom(directory)`
— the last two so a project running the catalog without the xUnit package gets the same behaviour.
Unlike the Java side there is still no provider SPI: a framework without a preset is built up in code
from `FrameworkTypes.None()`.

**Corrected: why `dca.rules.freeze` is refused.** The message claimed ArchUnitNET has no equivalent of
ArchUnit's `FreezingArchRule`. It does — `ArchUnitNET.Fluent.Freeze` ships `FreezingArchRule`,
`IViolationStore`, `JsonViolationStore` and `XmlViolationStore`. The real obstacle is this library:
`IDcaRule` does not expose the ArchUnitNET rule behind a check, so there is nothing to build a baseline
from. The message and the README now say that instead.

**`DCA-ERR-002` and `DCA-ERR-003` no longer both report the same exception.** A subtype of
`DomainException` declared in an application namespace was reported twice, with contradictory
remedies. `DCA-ERR-002` owns the case; `DCA-ERR-003` no longer selects domain-exception subtypes.

**No logging library in the domain by default.** `Microsoft.Extensions.Logging.Abstractions` leaves the
`DCA-ONI-002` allow-list; the Java twin never had a logging facade in it, and one rule id cannot mean two
policies. **This can turn a green build red** — a domain model that logs adds the namespace back with
`WithThirdPartyNamespacesAllowedInDomain(...)`, which is the supported way to make that decision.

**The four fixed method names are written down.** `SaveAsync`, `DeleteByIdAsync`,
`PublishAndClearEventsAsync` and `RegisterEvent` are matched by name and are not marker roles. The three
`Checks` texts and the `DcaMarkers` documentation now say so.

**`DCA-NAM-006` now expects `*Resource`, as `dca-archunit` always did.** The same rule id demanded
opposite class names in the two libraries: `Resource` in Java and the conventions the guide states,
`Controller` here — so the same story produced differently named classes in the two reference samples,
and the .NET sample only passed because its architecture test overrode the default back to `Resource`.
The .NET default is now `Resource` too. **This can turn a green build red**: an ASP.NET Core code base
that follows the platform's own convention restores it with
`WithRestControllerSuffix("Controller")`, one line in the layout. `ControllerSuffix`, which governs MVC
and Razor page models, is unchanged at `Controller`.

**The packages carry the version they will ship as, and `rules.json` says which release its texts
belong to.** `DomainCentric.ArchRules` and `.Xunit` are `0.5.0`, `DomainCentric.BuildingBlocks` `0.2.0`
for the two new exception base types. `rules.json` gained `library` and `version` at the top, read from
the assembly that produced the entries. The README now states the public API surface, the versioning
promise after 1.0 — a new or tightened rule is a major bump — and the exact rule counts.

**xUnit v3 no longer builds green while nothing runs.** `DomainCentric.ArchRules.Xunit` builds on
`xunit.core` 2.x. On a v3 project it restored and compiled without a warning and then contributed no
test: `dotnet test` exited 0 and all 123 architecture rules silently did not run. The package now ships
a build target that stops such a project with error `DCA0001` and points at the supported path —
`DomainCentric.ArchRules` alone plus `DcaRules.CheckAll` in a test of your own, which works on any
framework. Set `DcaArchRulesXunitSkipVersionGuard=true` to build anyway. Both READMEs gained a
compatibility table.

**New rule `DCA-STR-012` — at least one module owns a DCA layer.** The third guard of the same kind as
an empty import and an undeclared bounded context. A code base whose layers are named `core` and `usecases`
instead of the configured segments yields no module root, so every rule that selects over the layers — the
whole use-case set among them — matched nothing and the suite was green. The violation names the configured
segments, the namespaces it did see, and the way out. This is the most likely first run of a code base that has
not configured the layout, so **it can turn a green build red**; switch it off with a recorded reason if the
code base deliberately has no layered module.

**`DCA-ONI-002` no longer reports a project's own marker vocabulary.** The rule named the two
building-blocks namespaces directly, so a project that pointed the roles at its own markers was told
its aggregate root was a forbidden third-party dependency inside its own domain. The allowed
namespaces are now derived from the configured roles
(`DcaMarkers.DeclaringNamespacesOf(DcaMarkers.DomainFacingRoles)`); with the default roles this
yields exactly the two namespaces that were written in by hand. Matches `dca-archunit`.

**Every violation now names its rule.** A report line used to carry the offending element and, for the five
`DCA-ERR` rules, the id written into the header by hand; the other rules named it nowhere. `DcaRuleExecution`
now prefixes every line of every report with `[DCA-XXX-nnn]`, matching `dca-archunit`. Nothing about a rule's
selection, check or severity changes. A consumer that parses report text sees the new prefix.

**What can turn a green build red:**

- `DCA-ERR-004` also forbids the new `TransportStatusAttributeTypes` role — full attribute type names that
  fix the protocol answer of the type they sit on. Empty in every preset, because the platform answers
  through a mapper type rather than through an attribute on the failure; a project that has such an
  attribute of its own configures it and a domain or use-case exception carrying it turns red. The twin
  setting in `dca-archunit` is the `transportStatus` role.

- **The rules no longer reference the building-block markers as types.** Every selection that used to name
  `IAggregateRoot`, `IEntity`, `IValue`, `IId`, `IDomainEvent`, `IIntegrationEvent`, `IDomainService`,
  `IFactory`, `DomainException`, `UseCaseException`, `ITransactionBoundary`, `IInputPort`, `IUseCase`,
  `IOutputPort`, `IRepository`, `IStore`, `IDomainEventPublisher` or `IIntegrationEventPublisher` now asks
  the layout for that *role*, resolved by full type name through the new `DcaMarkers`. `DcaLayout` defaults
  to `DcaMarkers.Dca()`, so nothing changes for a project on the building blocks; a project with its own
  markers points the roles at its own types (`WithMarkers(DcaMarkers.Dca() with { AggregateRoot =
  "Acme.Platform.IContractRoot" })`) and is governed by the whole catalog instead of excluding the affected
  rule ids. A role must name a type — an empty one is refused rather than silently selecting nothing. What
  the roles do not cover are the strategic attributes: the rules read their members, and a name does not
  carry members.
- Correcting the first cut of this change, found in review: the `UseCase` and `TransactionBoundary` roles had no
  reader at all — `DCA-NET-003`, `DCA-NET-006`, `DCA-NAM-002` and the transaction-coverage check still matched
  `IUseCase<,>` and `ITransactionBoundary` as types, so configuring either role changed nothing. Both now
  resolve through the role, generic arity included (`DcaMarkers.IsRole` / `CarriesRole`). The exclusion of the
  vocabulary's own code in the error rules was hard-wired to this library's namespace and is now derived from
  the namespaces the configured role types live in, so a project's own exception base type is no longer
  reported as residing outside its layer.
- `DcaMarkers.Validate()` states what the layout does with a vocabulary when it is built, instead of calling
  `Roles()` for its side effect, and it also refuses a blank `Name` — which only the Java library did.
- `DcaLayout.MarkersReport()` names the vocabulary and the roles that differ; `ToString()` carries it. The
  xUnit base class has no diagnostic case of its own, unlike the Java library's, and the documentation says so
  rather than promising a report.
- `DCA-USE-015` names the configured aggregate-root and entity roles in its violation instead of the
  literal `IAggregateRoot` / `IEntity`, so the message stays true under a project's own vocabulary.

- The `errors` rule set is new, `DCA-ERR-001` … `DCA-ERR-006`, and pins the two new building blocks
  `DomainException` and `UseCaseException`. A project that declares its own exception types in the domain or
  application layer will see `DCA-ERR-001` / `DCA-ERR-003` until those types derive from the base type of their
  layer; `DCA-ERR-002` adds the reverse direction (a subtype declared outside its layer), `DCA-ERR-004` forbids
  classified container, persistence and transaction metadata on them, and `DCA-ERR-005` forbids the technical
  suffixes `Error`, `Fault`, `Failure` and the words `Http`, `Status`, `Response` in their names. Argument guards
  are outside all of this: the platform's own argument exceptions are never selected. `DCA-ERR-006` is
  informational - the type model carries neither a `throw` nor a `catch`, so an adapter that catches a generic
  exception can only be listed, never failed. It reports an incoming adapter *namespace*, not a single type: a
  central exception handler beside the adapters is what the doctrine asks for, and a per-type diagnostic would
  list exactly the adapters that use one.

- `DCA-STR-011` is new: at least one namespace below the root namespace declares `[BoundedContext]`. Without a
  declaration the context-map and isolation rules select nothing and report success over an empty model. A code base
  that deliberately declares no context switches the rule off with a recorded reason; the structural isolation rules
  keep governing the modules.
- `DcaArchitecture.Load` refuses a run whose assemblies hold no type below the root namespace, instead of running
  every rule over nothing. The message names the root namespace and the assemblies that were passed.
  `DcaArchitecture.Of(...)` is unchanged - an explicitly built model is the caller's.
- `DcaRules.Only(layout, ...)` rejects an unknown rule-set name and `DcaRules.AllExcept(layout, ...)` an identifier
  that is neither in the catalog nor retired; both used to select or exclude nothing silently. `DcaRuleSelection`
  validated these all along.

- `DCA-LAY-004` also sees programmatic boundaries beyond `TransactionScope`, through two new `FrameworkTypes` roles.
  `TransactionApiTypes` (the types code runs a transaction with; the preset lists `CommittableTransaction`,
  `IDbTransaction`, `DbTransaction` and the persistence library's `IDbContextTransaction`; `TransactionScope` stays
  the positional setting beside it) is allowed exactly in the application layer and the outgoing adapters; any other
  type that depends on one is reported, a bootstrap runner in the global infrastructure namespace included.
  `TransactionManagerTypes` (a project's own manager abstraction; empty by default) and `ITransactionBoundary` are
  wiring and plumbing: the global and the shared kernel's infrastructure namespaces may depend on them too; the
  domain, incoming adapters and a module's own infrastructure may not. Implementations of `ITransactionBoundary` are
  exempt everywhere.
- `DCA-HEX-007` title and texts now describe the code: "Incoming adapters depend on no other module, except event
  consumers on the events they subscribe to". An incoming adapter may not depend on any namespace of another isolated
  module, published `Api`/`Events` included; the single exemption is the event-consumer segment. Behaviour unchanged.
- `DCA-MAP-008`, `DCA-MAP-009` and `DCA-MAP-010` separate two questions: placement of code that exists is checked
  for every declaration, Planned included; only an Implemented `[Upstream]` demands that a translation site exists
  (`DCA-MAP-008`, as `DCA-MAP-007` demands an implementation). `DCA-MAP-011` keeps counting a Planned declaration as
  declared.
- `DCA-TAC-002` inspects every field and property, static ones included - it used to skip statics, unlike the Java
  twin; a static port breaks persistence ignorance just the same. `DCA-TAC-003` inspects instance members only (a
  static same-type member holds no aggregate). Both `Checks` texts say so.
- `DCA-CYC-001..004`: the `Checks` texts state the limit - slices are per module root, a cycle inside one module's
  layer is not detected there, `DCA-CYC-005` covers the application layer per operation. No code change.
- Two more layout segments: `WithModelSegment` (default `Model`) and `WithIncomingEventSegment` (default `Event`).
  `DCA-CYC-001` and the domain-model patterns read the first, the event-consumer exemption of `DCA-HEX-006` and
  `DCA-HEX-007` the second; neither is hard-coded any more. `DcaLayout.IncomingEventAdapterPattern` and
  `DomainModelNamespaceOf(module)` are new.
- `DCA-ONI-002`: `Checks` now states, as the Java twin does, that the shared kernel is allowed only when it is a
  module root with a `Domain` layer of its own; no behaviour change.
- `DCA-STR-007`: a test proves that a renamed Events segment (`WithEventsSegment`) is honoured; the rule has read the
  segment from the layout since 0.4.0, nothing in it is hard-coded.

## [0.4.0] - 2026-09-10

**Migration from 0.3.0.** Depends on `DomainCentric.BuildingBlocks` 0.1.1 (documentation only). Before 1.0 a minor
version may add and tighten rules; what can turn a green 0.3.0 build red — same contract as `dca-archunit` 0.4.0:

- `DCA-USE-012` now demands the transaction boundary for every use case that **saves or deletes** an aggregate, not only
  for one that publishes — wrap the work in `ITransactionBoundary.InTransactionAsync(...)` or carry the configured
  transactional attribute (`FrameworkTypes.TransactionalAttribute`, empty by default).
- `DCA-USE-016` (new): a use case may not invoke another use case — directly, through its input port or through an
  application helper. `DCA-USE-017` (new): the public surface of a use case is its input port; a `*UseCase` that
  implements no `IInputPort` is reported in full — implement the port.
- `DCA-NET-003` validates every `IUseCase<TIn,TOut>` implementation through the interface map and still fails an
  `ExecuteAsync` declared without the generic contract.
- `DCA-MAP-008` wants one translation site per declared ACL interaction; `DCA-CYC-005` also sees cycles between use-case
  namespaces inside one feature.
- Three ids are retired and cannot be selected any more (`OnlyIds` throws with the replacement): `DCA-TAC-022`
  (→ `DCA-TAC-014`), `DCA-MAP-003`, `DCA-ADV-003` (→ `DCA-ADV-001`).
- `FrameworkTypes` is a positional record with `Name` first; callers of the presets are unaffected.

Relaxed at the same time: `DCA-HEX-005` allows global and own-module infrastructure in outgoing adapters; `Manager` is a
valid domain term (`DCA-NAM-010`); a `*Response` may live in an outgoing adapter (`DCA-USE-008`); a Repository or Store used
by one use case may live with it (`DCA-TAC-014/019`); `Store.FindByIdAsync` is allowed (`DCA-TAC-021`); domain-metadata
rules allow unclassified attributes instead of reporting every unknown one; a `Version` property that is a business
revision passes `DCA-ADV-006/007`; and `DCA-USE-009` exempts a use case whose aggregate provably never registers an event.

### Added

- **Informational rules.** A rule may be `informational`: it runs and reports but never fails the build (`DCA-LAY-005`,
  `DCA-STR-009/010`, `DCA-MAP-011`). `rules.json` carries `status`; `RULES.md` and the counts separate enforced (112)
  from informational (4), n/a (3) and retired (3).
- **Retired rules.** `rules.json` gets a `retired` registry (id, since, reason, replacement); `DcaRuleSelection.RetiredReferences`
  lists the retired ids a configuration refers to, the xUnit base prints one notice per id, `OnlyIds`/`dca.rules.ids` with
  a retired id throws. Ids are never reused.
- `DCA-USE-012` — ported (was n/a): every entry path to a save, delete or publication crosses a unit calling
  `ITransactionBoundary.InTransactionAsync` (extension overloads included) or carrying the configured transactional
  attribute; calls are read from the IL of the class and its state-machine and closure types, so `async` and lambdas are
  followed. Violations name the effect (`saves an aggregate`, `deletes an aggregate`, `publishes domain events`).
- `DCA-USE-016` — use cases do not invoke use cases; `DCA-USE-017` — the effective public surface maps onto the input
  ports (inherited and explicit implementations pass; unrelated methods and public properties fail, a property once).
- `DcaLayout.WithOperationContainers(...)`: optional organisational segments removed before measuring flat/grouped
  use-case depth (`DCA-USE-014`).
- Domain-metadata rules (`DCA-ONI-003`, `DCA-ADV-004/011/015/018`) classify the configured **roles** on types and members
  through `FrameworkTypes` attribute namespaces plus `PersistenceAttributeTypes` (`AspNetCore()` classifies `KeyAttribute`,
  `TimestampAttribute`, `ConcurrencyCheckAttribute`); unclassified attributes are allowed; ownership is exclusive.
  Mappings of other persistence libraries need a preset extension (`with { PersistenceAttributeNamespaces = ... }`).
- `DCA-USE-009` proves the event-free exemption: an `IRepository<T,ID>` whose aggregate hierarchy is fully under scan and
  registers no event anywhere needs no publication; unresolved arguments and partial scans keep the requirement.
- `DCA-NET-004`: readonly struct values; class and struct results are selected by `DCA-USE-015`.
### Added (framework-neutral vocabulary, WP-30)

- `FrameworkTypes.None()` — every role empty, for a hand-hosted application or a framework without a preset;
  controllers are then recognised by suffix only and `DCA-LAY-004` has nothing to look for. `FrameworkTypes.Name`
  (`aspnetcore`, `none`) is part of `DcaLayout.ToString()` so a report names the preset in use;
  `FrameworkTypes.IsSet(role)` is the empty-role test the rules use. Adjust a preset with a `with` expression.
- `FrameworkNeutralityTests`: fails the build when a rule's title, rationale, `Selects` or `Checks`, a `NotApplicable`
  reason, or a building-block XML-doc sentence names a framework (`Spring`, `EF Core`, `MediatR`, `@Service`, …) or
  shop vocabulary outside a sentence marked as an example. Same guard as the Java twin.

### Changed

- Immutable-shape rules (`DCA-USE-004/005/007`, `DCA-ADV-001`, `DCA-STR-008`, `DCA-TAC-010`) check shallow immutable state on
  classes, records and structs: inherited fields readonly, properties get-only or init-only, no `Set*` method. `DCA-USE-015`
  rejects same-type and marker-interface aggregate references and walks wrappers, part records and structs.
- Controllers (`DCA-HEX-003`, `DCA-NAM-005/006`) are selected by the configured roles or suffixes.
- `DCA-STR-007`: integration-event contracts reside in the configured `Events` segment; `DCA-ADV-006/007` distinguish a
  schema version from a business revision. `DCA-TAC-005` checks caller roles and context for entity construction.
- Same-id titles aligned with the Java twin (`DCA-TAC-010/014/019`, `DCA-NAM-010`, `DCA-USE-012`).

- `FrameworkTypes` is a positional record with `Name` first: `new FrameworkTypes(name, controllerBase,
  apiControllerAttribute, pageModelBase, transactionScope)`. Callers of the presets are unaffected.
- `HexagonalRules.IsController`, `NamingRules` (`IsMvcController`, `IsApiController`) and `DCA-LAY-004` treat an
  empty role as "matches nothing" instead of comparing against an empty name.
- The `NotApplicable` reasons of `DCA-NAM-002`, `DCA-USE-012/013` and `DCA-MAP-006` explain the Java rule in role
  terms ("injectable stereotype attribute", "declaratively transactional", "module declaration") instead of naming
  Spring; XML docs likewise. `rules.json` / `RULES.md` regenerated (116 rules, 4 n/a — unchanged counts).

### Retired

- `DCA-TAC-022` (covered by `DCA-TAC-014`), `DCA-MAP-003` (renderer disambiguation instead of a forced rename),
  `DCA-ADV-003` (duplicate of `DCA-ADV-001`). Listed in `rules.json` `retired`.

### Fixed

- `DcaArchitecture.Load` imports a consumer assembly's types in the reserved `DomainCentric.BuildingBlocks.Hexagonal.Ports.Out`
  namespace as well, so `DCA-LAY-005` reports a consumer implementation placed there through `DcaArchitectureTest` (before,
  only a hand-built `ArchLoader` reached them and the rule passed vacuously).

- The repository `Dockerfile` copies `LICENSE` into the build context; `dotnet pack` inside the image failed on the
  missing file the packages embed (`Directory.Build.props`). `podman build` is green again.

## [0.3.0] - 2026-09-08

Depends on `DomainCentric.BuildingBlocks` 0.1.0 (unchanged).

### Added

- Every rule describes its mechanics: `IDcaRule.Selects` names the types the rule looks at, `IDcaRule.Checks`
  what it asserts about them - including what does not count and what is deliberately not established. Twin of
  `dca-archunit`'s `selects()`/`checks()`, same ids, same wording wherever the .NET reading is the same;
  `rules.json` carries both as `selects`/`checks`, `RULES.md` shows them as two columns.

- `DcaLayout.WithControllerSuffix(string)` (default `Controller`): the suffix of MVC controllers and page models is
  configurable like the REST one. `DCA-NAM-005` checks it, `DCA-HEX-003` selects controllers by either suffix (plus
  base class and `[ApiController]`). Previously `Controller` was hard-coded in both rules.
- `DCA-USE-009`'s rationale states what its check always did: only `PublishAndClearEventsAsync` counts as a
  publication; `PublishAsync(event)` per event, even followed by `ClearDomainEvents()`, is reported. A fixture pins it.

### Fixed

- Twelve rules still selected through the layout's one-segment wildcard patterns (`Root.[^.]+.Domain.Model` and
  siblings) and therefore saw only modules that are direct children of the root namespace: `DCA-TAC-001`,
  `DCA-TAC-009`, `DCA-TAC-013`, `DCA-TAC-014`, `DCA-TAC-015`, `DCA-TAC-019`, `DCA-TAC-020`, `DCA-TAC-022`,
  `DCA-NAM-007`, `DCA-NAM-008`, `DCA-NAM-010`, `DCA-USE-008`. They now select over `DcaArchitecture.ModuleRoots()`
  (`AllDomainModelPatterns()` etc.) like every other rule, so a module two segments deep (`Acme.Shop.Sales.Order`)
  is governed. A flat layout sees no difference. `DCA-ONI-003` and `DCA-TAC-001/009` no longer add the whole
  `SharedKernel.Domain` namespace on top - the shared kernel is a module root when it owns a domain layer, and
  `Domain.Model` is what the Java twin checks.
- `DCA-STR-002` evaluated one fluent rule per bounded context and threw at the first context with violations;
  it now collects all contexts (`EvaluateAll`) and reports them together, as the Java twin does.
- Parity with `dca-archunit` (each a real difference in what was reported, not wording): `DCA-TAC-022` selects
  interfaces named `Enriched*` too (reported, since an interface is no record); `DCA-ONI-002` no longer lets the
  domain depend on the strategic annotations or the input ports just because the default allow-list names the
  whole `DomainCentric.BuildingBlocks` namespace - of the building blocks only `Ddd.Tactical` and
  `Hexagonal.Ports.Out` are allowed; `DCA-NAM-001` selects only classes that implement `IUseCase<,>`, not every
  class assignable to `IInputPort`; `DCA-USE-015` walks non-public members as well and skips nested classes;
  `DCA-STR-007` no longer selects interfaces that extend `IIntegrationEvent`; `DCA-STR-008` checks `record struct`
  events (previously never selected because structs are no `Class` in the model).

### Changed

- **Rule authors:** `DcaRule.Of(...)` and `DcaRule.Check(...)` return `DcaRule.Undescribed`; the rule is
  completed with `.Selecting(string).Checking(string)`, both mandatory (blank text throws). Consumers that only
  run the catalog (`DcaArchitectureTest`, `DcaRules.CheckAll`) are unaffected.

## [0.2.0] - 2026-09-07

**Migrating from 0.1.0.** Four things can break a consumer; everything else is stricter enforcement
of the same rules and new rules that a 0.1.0 code base may fail.

1. `dca.rule.<id>.ignore` holds **one** regular expression. Several expressions move to indexed keys
   (`.ignore.1`, `.ignore.2`, …) or are joined with `|`.
2. A context is identified by its namespace **relative to the root namespace** (`Sales.Order`, not
   `Order`). `[Upstream("…")]`, `[Partnership("…")]` and ignore patterns naming a nested context change
   accordingly; contexts that are direct children of the root are unaffected. `SimpleContextName` is
   obsolete.
3. Contexts are discovered by `[BoundedContext]` at any depth and the rules select over discovered
   modules — a context that the one-segment wildcard used to skip is governed now.
4. Four new rules (`DCA-USE-014`, `DCA-USE-015`, `DCA-HEX-012`, `DCA-CYC-005`) and the tightened
   `DCA-TAC-003/-007/-008`, `DCA-USE-009`, `DCA-LAY-003`, `DCA-HEX-004/-005`, `DCA-MAP-001` and
   `DCA-STR-003/-004/-006` can report code that passed 0.1.0. Lower a rule to `Warning(...)` while you
   fix it — `dca-archunit.properties` documents the reason.

Before 1.0 a minor version may tighten rules; each such change is listed under *Changed — breaking*.

### Added
- **Shaping the result — two rules** (ported from `dca-archunit`, same ids, titles and rationales). A use case
  result carries the answer, never a handle on the model; an incoming adapter formats that answer and obtains
  no domain collaborator of its own. Outgoing adapters are deliberately outside both rules — repositories and
  persistence mappers construct and reconstitute domain objects while implementing output ports.
  - `DCA-USE-015` — use case result models must not expose aggregate roots or entities. Selects every
    `*Result` class in an application namespace that is not an `IValue`, resolves its runtime type and walks
    its public properties and fields **transitively**: through generic type arguments (`IReadOnlyList<T>`,
    `IReadOnlyDictionary<K,V>`), `Nullable<T>`, arrays, records nested in the result and part records — record
    classes and record structs — anywhere in the application layer, `Application.Shared` included (parts carry
    no `Result` suffix); a generic part record is walked through its type arguments and its own members. Every member assignable to `IAggregateRoot` or `IEntity`
    is reported with its path (`ListOrdersResult.Latest -> OrderView.Order : Order (IAggregateRoot)`).
  - `DCA-HEX-012` — incoming adapters must not depend on domain services. Every class in an incoming adapter
    namespace (event consumers included) is checked for dependencies on a type assignable to
    `IDomainService`, through the ArchUnitNET dependency model and the runtime type's constructor
    parameters. The mechanical subset of "an incoming adapter derives no business facts"; construction of
    domain objects and calls into domain behaviour stay review checks, because a result may legitimately
    carry a domain `IValue` or read model the adapter has to name.
  Catalog: 116 rules (110 ported + 6 .NET-only, 4 Java rules n/a).
- **Features within a bounded context — two rules and a compatibility fixture** (ported from `dca-archunit`,
  same ids, titles and rationales). A *feature* is an optional, domain-named group of related use cases below
  a module's application namespace (`Application.<Feature>.<UseCase>`, e.g.
  `Checkout.Application.Session.StartCheckout`) — a navigation and cohesion boundary inside one bounded
  context, not a layer, module, aggregate owner or deployment unit; nothing in the library infers bounded
  contexts or aggregate ownership from it. The pre-existing rules already saw such namespaces (they select
  "below the layer"); `Fixtures/Features/CompatFixture.cs` runs the whole catalog against a grouped context
  so a later change of a selector into a direct-child assumption fails there first.
  - `DCA-USE-014` — use case namespaces within a module must use one consistent depth: flat
    (`Application.<UseCase>`) or grouped (`Application.<Feature>.<UseCase>`). Selects the classes ending in
    the configured `UseCaseSuffix`, ignores `Application.Shared`, abstract classes and nested types (async state
    machines included), and reports every offending module and namespace in one violation. A module without use
    cases is valid; a single use case may use either depth. Legibility only.
  - `DCA-CYC-005` — the immediate child namespaces of a module's application namespace (`Shared` excepted)
    must be free of cycles: features in a grouped layout, use cases in a flat one. Uses the same
    deterministic slice graph as `DCA-CYC-001..004`, with slices assigned from `ModuleRootOf(...)` and the
    first namespace segment below `Application`.
  Catalog: 114 rules (108 ported + 6 .NET-only, 4 Java rules n/a).
- **Context discovery at any depth.** `DcaArchitecture.RootContextNamespace` walks up from a type's
  namespace to the nearest ancestor whose marker class carries `[BoundedContext]` or `[SharedKernel]`,
  so a context may be grouped (`Acme.Shop.Sales.Order`) or be the root namespace itself. A context is
  identified by its namespace relative to the root (`ContextName`: `Sales.Order`), which is what the
  context-map rules, the renderer and rule messages now use; `SimpleContextName` is obsolete. For a
  context that is a direct child of the root the identifier is unchanged.
- **`DcaArchitecture.ModuleRoots()` — structural module discovery.** A module root is the shortest
  namespace prefix whose remainder starts with a layer segment (`Domain`, `Application`, `Adapter`),
  found at any depth and without an attribute. Distinct from `BoundedContexts`: being a context is a
  strategic declaration, owning a layer is a structural fact, and the layer rules apply to both.
  Plus `ModuleRootOf`, `LayerSegments`, `IsolatedModuleRoots` (module roots minus the shared kernel),
  `ModuleRootPatternsExcluding`, `PublishedPatternsExcluding`, and `AllDomain/DomainModel/Application/
  SharedOutputPort/Adapter/IncomingAdapter/OutgoingAdapterPatterns()` over module roots.
  `AllDomainPatternsWithSharedKernel` / `AllDomainModelPatternsWithSharedKernel` are obsolete aliases.
- **`DcaLayout.WithApiSegment()` / `WithEventsSegment()`** — the published-contract segments are layout
  settings like every other segment (defaults `Api`, `Events`; the two must differ), read via
  `ApiSegment`, `EventsSegment`, `PublishedSegments`. `DCA-STR-005/006/007`, the context-map rules and
  `ContextMapRenderer` take them from the layout; nothing hard-codes the channel names any more.
  Also `DcaLayout.AnyOf(patterns)` (alternation; empty → matches nothing) and `AnySegmentPath`.
- `DcaRule.EvaluateAll(rules, arch, title, rationale)` — evaluates several fluent rules that make up one
  DCA rule and throws once with all their violations.

### Changed — breaking
- **An `.ignore` property value is one regular expression.** `dca.rule.<id>.ignore` was split on commas
  like a list of rule ids, so `Foo.{1,3}Bar` failed as an invalid expression. The value is now taken as
  written; a second expression for the same rule uses an indexed key (`.ignore.1`, `.ignore.2`, …, applied
  after the unindexed one). Same semantics as `dca-archunit`.
- **`DCA-USE-009` checks the entry path, not the class.** The rule asked only that a `SaveAsync` and a
  `PublishAndClearEventsAsync` call exist somewhere in the use case class, so a publication in an unrelated
  method covered a saving method. It now follows the directed calls within the class (`IntraClassCalls`,
  built from the runtime type's IL — the compiler's async state machines and lambda closures are units of
  the graph, joined to the method that declares them): for every unit that calls `SaveAsync`, every entry
  point reaching it must also reach a `PublishAndClearEventsAsync`. An entry point is a unit callable from
  outside the class — any non-private method or constructor written in source, so a public method stays an
  entry point even when another method of the class also calls it (compiler-generated units are not) — or
  one nothing in the class calls.
  An entry method may save through one helper and publish through another, over any number of steps; a
  helper two entry methods share does not connect them; a saving helper shared by a publishing and a
  non-publishing entry method is reported for the latter (`Foo.ExecuteQuietlyAsync (via PersistAsync)`); a
  public `ExecuteAsync` that only saves is reported even when a public `CompleteAsync` calls it and publishes
  afterwards; recursion terminates. Rationale identical to the Java rule, which reasons the same way; order of the two
  calls and the identity of the aggregate stay outside the check. Fixtures: `Fixtures/Transactions`.
- **`DCA-TAC-003`, `-007`, `-008` see arrays and generic base classes.** The member walk took the generic
  arguments from the ArchUnitNET model only, so `Order[]` in an aggregate, entity or value object passed, and a
  member inherited from a generic base (`class Base<T> { T Value; }`, `Order : Base<Customer>`) was an open
  type parameter. `DataMembers` now adds what reflection sees in the inspected type's context — array element
  types, generic arguments recursively, the concrete type a base's parameter is bound to. `DCA-TAC-003` keeps
  its one tolerance exactly: a direct member of the own type passes; a container of the own type
  (`IReadOnlyList<Category>`, `Category[]`, `IReadOnlyDictionary<string, Category>`, nested lists) holds
  *other* instances and is reported — as the Java rule does.

### Changed
- **Rules select over discovered modules, not over a one-segment wildcard.** Every rule that used
  `Layout.DomainPattern` and its siblings (`Root.[^.]+.Domain`, exactly one segment) now selects through
  `arch.All*Patterns()` built from `ModuleRoots()`. A context grouped or nested one level too deep used
  to match no rule and pass silently; it is governed now. The wildcard properties remain for tooling.
- **Isolation is structural.** `DCA-STR-003`, `DCA-STR-004`, `DCA-STR-006` and `DCA-HEX-007` iterate over
  `IsolatedModuleRoots()` on the source and the target side, so a module that declares no
  `[BoundedContext]` can neither reach into a neighbour's internals nor have its own reached into. The
  adapter allow-list is the target's `Api`/`Events` namespaces; `DCA-STR-006` forbids everything else in
  a foreign module (adapters and infrastructure included). Titles and rationales say "module"; the four
  rules report every offending module, not only the first. Same ids and texts as `dca-archunit`.
- **`DCA-STR-005`** accepts an Open Host Service in `Api` or anywhere under `Adapter.Incoming` — the
  pattern is the published relationship, not a folder; the former `Adapter.Incoming.OpenHost` requirement
  is gone.
- **Cycle rules slice by module root.** `CycleRules.CheckSlices` assigned slices with a one-segment
  regex capture, so two contexts grouped below an intermediate namespace produced no slices and a cycle
  between them went unreported. Slices are now `ModuleRootOf(namespace)`, at any depth.

### Fixed
- **`DCA-MAP-001` sees declarations on nested namespaces.** A `[Partnership]` on a marker class in
  `Cart.Application.GetCart` was neither reported nor rendered; the rule inspected the resolved context
  roots only. It now inspects every namespace below the root that a loaded type lives in, ancestors
  included (`DcaArchitecture.NamespacesBelowRoot()`), and requires the declaring namespace itself to carry
  `[BoundedContext]`.
- **`DCA-USE-015` includes inherited members and reports every path.** A `sealed class ArchivedOrdersResult
  : BaseListing` inherited an aggregate property from a base class without the `Result` suffix and passed
  (`BindingFlags.DeclaredOnly`); and a global visited set reported only the first path through a part
  record. The walker now reads all public instance members and keeps only the records on the current path,
  so `FirstLine -> LineView.Line` and `LastLine -> LineView.Line` are both reported.
- **`DCA-NAM-011` works for grouped and single-context layouts.** The rule built
  `Root.<segment>.Adapter.Incoming.Web`; the allowed namespaces are now derived from the module roots.
- **Infrastructure is selected at both levels.** `IsInfrastructureImplementation` looked at the global
  `Root.Infrastructure` only. `DcaArchitecture.InfrastructureNamespaces()` lists it plus every isolated
  module's own `Infrastructure` namespace (the shared kernel's excluded — shared support, not one module's
  detail); `DCA-LAY-002`, `-003`, `DCA-HEX-004` and `-005` use it.

## [0.1.0] - 2026-09-07

Feature parity with `dca-archunit` 0.1.0 (same rule ids and rationales; 4 Java rules not applicable, 6 .NET-only rules).

### Added
- Initial .NET port of `dca-archunit` (Java) on ArchUnitNET: same rule ids `DCA-<SET>-<NNN>`; `DcaLayout`, `DcaArchitecture`, `DcaRule`, `DcaRules`, `ContextMapRenderer`; xUnit base class `DcaArchitectureTest`.
- .NET-only rule set `dotnet` (`DCA-NET-001…`): synchronous domain, `Async` suffix on port methods, one `ExecuteAsync` per use case, records for values and ids.
- **Configurable rule selection.** `DcaRuleSelection` decides which rules run and how strictly:
  `OnlySets` / `OnlyIds` narrow the run, `Excluding(id, reason)` switches a rule off,
  `Warning(id, reason)` reports it without failing the build, and
  `IgnoringViolationsMatching(id, regex)` tolerates a documented exception. Readable from
  `dca-archunit.properties` next to the test assembly (same keys as the Java library), which the xUnit
  base class reads and merges `AdditionalSelection` on top of — override that property, not
  `Selection`, which would replace the file. Unknown rule ids and set names fail the run.
  No baseline dial: ArchUnitNET has no `FreezingArchRule`, and `dca.rules.freeze*` is rejected with a
  message pointing at `dca.rules.warn`.
- **New public API:** `DcaSeverity`, `DcaRuleSelection`, `DcaRuleExecution`, `DcaRuleOutcome`,
  `DcaRules.Select/SelectFlat/SetNames/SetOfRule`, `DcaRules.CheckAll(architecture, selection)`, plus
  `Header`/`Violations`/`Retaining` on `DcaRuleViolationException`. `ExcludedRuleIds` and `Rules` keep
  working.
- `DCA-HEX-011` — incoming adapters must depend on input port interfaces, not on use case classes (ported from `dca-archunit`, same id). Injecting the concrete implementation couples the adapter to one realisation and defeats the Dependency Inversion Principle the port exists for. Catalog: 112 rules (106 ported + 6 .NET-only, 4 Java rules n/a).
- `DCA-NET-006` — application layer must not use persistence or transaction frameworks (EF Core, System.Data, System.Transactions, Dapper, NHibernate, MongoDB driver); the boundary is a decorator or the `ITransactionBoundary` port.

### Changed
- Theory cases are named by rule set (`tactical / DCA-TAC-001`), a rule the properties file lowers to
  `WARN` or `OFF` carries its severity and recorded reason in the display name, and a rule the file
  scopes out produces no case at all — the test count now says what was actually checked. Previously
  an excluded rule passed silently, which hid the decision; xUnit v2 cannot skip dynamically, so a
  lowered rule is still reported green. Settings made in `AdditionalSelection` take effect but cannot
  shape the display name, because theory data is built before an instance exists.
- `DCA-USE-013` (Java: transactional use cases must not call remote-capable ports) listed as not applicable; 4 n/a rules now.
- `DCA-USE-012` (Java: publishing use cases must be transactional) listed as not applicable in `UseCaseRules.NotApplicable`; the catalog now reports 3 n/a rules.
- Target frameworks: `net8.0;net10.0` (BuildingBlocks additionally `netstandard2.1`); tests, tools and the
  minimal consumer sample build on `net10.0`.

### Fixed
- `DCA-LAY-005` ignores compiler-generated nested types (closures, async state machines of default interface methods) in `Ports.Out`.
- `DCA-CYC-001…004` sliced with ArchUnitNET's `Slices().Matching("Root.(*).Layer")`, which ignores the segments after `(*)` and slices every sub-namespace of a context — all four rules reported the same intra-context pairs (e.g. `Domain.Model` ↔ `Domain.Event`) as cycles. The rules now build one slice per context from the layer's types only and search elementary cycles between slices themselves (Java semantics of `Root.(*).layer..`). Found by the first real consumer (`dca-ecommerce-sample-dotnet`).

- 2026-09-09 WP-34 (unreleased): NAM-002 Java diagnostic never fails (.NET n/a); HEX-005 permits own/global infrastructure; ONI-003 and ADV-004/011/015/018 share exclusive role-by-target metadata checks. Java gains injectionSite/persistenceMapping presets and composed detection; .NET gains attribute namespaces and base-attribute detection, replacing the allow-list. No wiring guarantee; no new marker. Shared catalog regeneration pending WP-37.

- 2026-09-09 WP-35 (unreleased): shared new IDs USE-016 (operation invocation, including helpers) and USE-017 (effective public input-port surface); CYC-005 slices operations inside features, respecting containers; MAP-008 requires per-interaction translation evidence without package exclusivity. NET-003 uses the generic interface map (inherited/explicit valid). No coordination marker; anchored caller-side ignore is the explicit exception. Counts await the shared regeneration.

### WP-36 (unreleased 0.4.0)

- `DCA-USE-009` permits event-free saves only with a resolved, fully inspected aggregate; unresolvable types remain checked.
- `DCA-USE-012` has the same id in both languages. Its static graph proves boundary evidence, not block containment.
- **Breaking migration from 0.3.0:** `DCA-STR-007` accepts only the configured events segment. Move contracts from
  adapter/outgoing/event to events, or temporarily exclude DCA-STR-007 by id during migration. Translators stay in adapters.
- `DCA-ADV-006/007` intentionally stop banning business `version`; the three explicit schema-version names are a heuristic.
- `DCA-HEX-006` is directional; `DCA-HEX-007` names integration events and published APIs correctly.


## Catalog kinds and retired identities (2026-09-09)

Catalog entries distinguish enforced rules from informational diagnostics: LAY-001, STR-001, STR-010, MAP-013,
and Java NAM-002. Test runners and generated catalogs report both counts separately. Informational entries do
not prove architectural correctness or runtime wiring. `kind()` / `Kind` is explicit metadata, independent of severity.

Retired ids are never reused: MAP-003 delegates normalized-name collision handling to the context-map renderer;
ADV-003 is covered by ADV-001's immutable-shape check; TAC-022 is covered by TAC-008..012 for value models,
with enrichment guidance in the guide/catalog. `DcaRules.retired()` / `Retired()` retain reason, replacement and
version. Properties exclusions/severity settings and programmatic exclusions using these ids keep loading and
are reported as retired (`DcaRuleSelection.RetiredReferences`, one line per referenced id in the xUnit base with reason
and replacement); selecting a retired id (`OnlyIds`, `dca.rules.ids`) fails naming the replacement. Unknown ids still fail. The change is intentional in unreleased 0.4.0 for 0.3.0 consumers.

USE-001 retains consumer redeclaration coverage; LAY-005 checks imported consumer implementations in the reserved
building-blocks output-port namespace/package. An imported original interface passes. Name-discovery rules remain:
unmarked types would otherwise evade marker-only selection. Current counts come from generated `rules.json`,
including status and the separate retirement registry, rather than a hard-coded expected total.
