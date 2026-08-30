# Porting `dca-archunit` (Java) to `DomainCentric.ArchRules` (.NET / ArchUnitNET)

Working spec for the port. Source of truth: `../dca-java/dca-archunit/src/main/java/dev/domaincentric/dca/archunit/rules/<Set>Rules.java`
plus `../dca-java/RULES.md` (ids, titles, rationales — the contract) and the Java fixtures under
`../dca-java/dca-archunit/src/test/java/dev/domaincentric/dca/archunit/fixtures/<set>/`.

## Target model (implemented in `src/DomainCentric.ArchRules/`)

| Java (`dca-archunit`) | .NET (`DomainCentric.ArchRules`) |
|---|---|
| `DcaLayout.forBasePackage(p)`; `basePackage()`, `domainSubpackage()`, … | `DcaLayout.ForRootNamespace(ns)`; `RootNamespace`, `DomainSegment`, `ApplicationSegment`, `AdapterSegment`, `IncomingSegment`, `OutgoingSegment`, `InfrastructureSegment`, `SharedKernelSegment`, `UseCaseSuffix`, `RestControllerSuffix`, `ThirdPartyNamespacesAllowedInDomain`, `FrameworkTypes` |
| `layout.domainPattern()` … (ArchUnit `..` patterns) | `layout.DomainPattern`, `DomainModelPattern`, `ApplicationPattern`, `SharedOutputPortPattern`, `AdapterPattern`, `IncomingAdapterPattern`, `OutgoingAdapterPattern`, `InfrastructurePattern`, `SharedKernelPattern`, `SharedKernelDomainPattern`, `SharedKernelDomainModelPattern` — **regular expressions** over full namespace names for `ResideInNamespaceMatching(...)` |
| `layout.domainPattern(ctx)` (per context) | `layout.DomainPatternOf(ctx)`, `DomainModelPatternOf`, `ApplicationPatternOf`, `SharedOutputPortPatternOf`, `AdapterPatternOf`, `IncomingAdapterPatternOf`, `OutgoingAdapterPatternOf` |
| `"pkg.."` literal | `DcaLayout.Below("Ns")` (ns and below), `DcaLayout.Exactly("Ns")`, `DcaLayout.IsBelow(full, prefix)`, wildcard segment `DcaLayout.Segment` |
| `DcaLayout.BUILDING_BLOCKS_*_PACKAGE` | `DcaLayout.BuildingBlocks*Namespace` constants (plain namespaces — wrap with `Below(...)`) |
| marker classes `AggregateRoot.class` … | `typeof(IAggregateRoot)` (non-generic base interfaces exist: `IEntity`, `IAggregateRoot`, `IRepository`, plus `IValue`, `IId`, `IDomainEvent`, `IIntegrationEvent`, `IDomainService`, `IDomainGateway`, `IFactory`, `ISpecification<>`, `IInputPort`, `IUseCase<,>`, `IOutputPort`, `IStore`, `IDomainEventPublisher`, `IIntegrationEventPublisher`; attributes `BoundedContextAttribute` (`.Ddd.Strategic`), `SharedKernelAttribute` (`.Ddd.Strategic.Relationships` — fully qualify it inside a `SharedKernel` namespace), `OpenHostServiceAttribute`, `UpstreamAttribute`, `ExternalUpstreamAttribute`, `PartnershipAttribute`, `IntegrationEventTypeAttribute`) — namespaces `DomainCentric.BuildingBlocks.Ddd.Tactical`, `.Ddd.Strategic`, `.Ddd.Strategic.Relationships`, `.Hexagonal.Ports.In`, `.Hexagonal.Ports.Out` |
| `arch.classes()` (`JavaClasses`) | `arch.Architecture` (ArchUnitNET `Architecture`) for fluent rules; `arch.Types` / `arch.Classes` / `arch.Interfaces` (only types below the root namespace) for hand-written loops |
| `arch.boundedContexts()` / `boundedContextPackages()` | `arch.BoundedContexts` (`IReadOnlyDictionary<string, BoundedContextAttribute>`) / `arch.BoundedContextNamespaces` |
| `arch.sharedKernelPackage()` (`Optional`) | `arch.SharedKernelNamespace` (`string?`) |
| `arch.packageAnnotation(pkg, T.class)` / `packageAnnotations` | `arch.NamespaceAttribute<T>(ns)` / `arch.NamespaceAttributes<T>(ns)` — read by reflection from the **marker class** in that exact namespace (`[BoundedContext("Cart")] public static class CartContext {}` — the .NET stand-in for `package-info`) |
| `arch.rootContextPackage(pkg)` | `arch.RootContextNamespace(ns)`; `DcaArchitecture.SimpleContextName(ns)` |
| `arch.boundedContextPatterns()` / `…Excluding(p)` | `arch.BoundedContextPatterns()` / `arch.BoundedContextPatternsExcluding(ns)` |
| `arch.contextDomainPatterns()` etc. | same names: `ContextDomainPatterns()`, `ContextDomainModelPatterns()`, `ContextApplicationPatterns()`, `ContextAdapterPatterns()`, `AllIncomingAdapterPatterns()`, `AllOutgoingAdapterPatterns()`, `AllDomainPatternsWithSharedKernel()`, `AllDomainModelPatternsWithSharedKernel()` |
| `arch.infrastructureImplementation()` predicate | `arch.IsInfrastructureImplementation(IType)` |
| `layout.frameworkAnnotations().service()` … (Spring names) | `layout.FrameworkTypes.ControllerBase` / `.ApiControllerAttribute` / `.PageModelBase` / `.TransactionScope` (ASP.NET Core names, matched by full name). **There is no .NET stereotype for `@Service`/`@Component`/`@EventListener`/`@ApplicationModule`** — rules that only exist to check such an annotation are **n/a** (see below). |
| reflection on runtime classes | `arch.RuntimeTypes()`, `arch.RuntimeType(IType)` → `System.Type` |
| `DcaRule.of(id, title, rationale, arch -> ArchRule)` | `DcaRule.Of(id, title, rationale, arch => IArchRule)` — evaluated with `Evaluate`; every failed `EvaluationResult` is one violation line; an empty selection passes |
| `DcaRule.check(id, title, rationale, arch -> { … })` | `DcaRule.Check(id, title, rationale, arch => { … })`; inside use `DcaRule.Evaluate(archRule, arch, title, rationale)` for fluent sub-rules and `DcaRule.Fail(header, violations, fix)` for collected violations. Violations throw `DcaRuleViolationException` (never `Xunit.Assert`) |
| `DcaRuleSet` | `IDcaRuleSet` (`Name`, `Rules`); class `<Set>Rules` in namespace `DomainCentric.ArchRules.Rules`, ctor `(DcaLayout layout)`, `Layout` property, one `public static IDcaRule` factory per rule (same method name as Java, PascalCase) |

ArchUnitNET 0.13.4 API you will use (`using static ArchUnitNET.Fluent.ArchRuleDefinition;`):
`Types()`, `Classes()`, `Interfaces()` → `.That().ResideInNamespaceMatching(regex)`, `.AreAssignableTo(typeof(X))`, `.ImplementInterface(typeof(X))`,
`.HaveNameEndingWith(s)`, `.HaveNameMatching(regex)`, `FollowCustomPredicate(t => t is not Interface, "are not interfaces")` (there is no `AreNotInterfaces()` on `Types()`), `.HaveAnyAttributes(typeof(A))`, `.DoNotHaveName(s)`,
then `.Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(regex)`, `.OnlyDependOnTypesThat()…`, `.BeAssignableTo(...)`, `.ImplementInterface(...)`, `.ResideInNamespaceMatching(...)`,
`.NotDependOnAny(...)`, `.BeSealed()` (classes), `.BeRecord()` may not exist — use `Class.IsRecord` in a loop, `.FollowCustomCondition(Func<T,bool>, description, failDescription)` for anything else.
`.Because(...)`/`.As(...)` exist but `DcaRule.Of` already formats title/rationale — don't duplicate.
Slices: `SliceRuleDefinition.Slices().Matching("Acme.Shop.(*)").Should().BeFreeOfCycles()`.
Facts verified against 0.13.4: an empty selection is reported as failed by ArchUnitNET ("requires positive evaluation") — `DcaRule.Of`/`Evaluate` already treat that as a pass, so you do not need `WithoutRequiringPositiveResults()`. `Classes()...Should().BeRecord()` / `.BeSealed()` / `.BeAbstract()` exist (class conditions only). `record struct`s are `Struct`s in ArchUnitNET (`arch.Architecture.Structs`), records are `Class.IsRecord == true`. An `init` accessor is reported as a **public setter** (`PropertyMember.SetterVisibility == Public`, method `set_X` with `MethodForm.Setter`) — a rule that forbids setters must exempt init-only accessors via reflection (`arch.RuntimeType(type)?.GetProperty(name)?.SetMethod?.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(System.Runtime.CompilerServices.IsExternalInit))`) and record-generated members (`EqualityContract`, `Deconstruct`, `PrintMembers`, `<Clone>$`, `op_Equality`…). `LoadNamespacesWithinAssembly` loads the namespace and everything below it. Dependencies of a type include compiler-generated attribute references (`System.Runtime.CompilerServices.*`) — allow `System` in domain allow-lists.
**Verify every method you use compiles** — build early, build often. Namespace matching is regex only (`ResideInNamespaceMatching`); a plain `ResideInNamespace(fullName)` matches the exact namespace and, apparently, its children — prefer the regex forms from `DcaLayout`.
Domain model: `Class` has `BaseClass`, `ImplementedInterfaces`, `Members` (`FieldMember` / `PropertyMember` (Getter/Setter/SetterVisibility) / `MethodMember` (Parameters, ReturnType, Visibility, `MethodForm`: Constructor/Getter/Setter/Normal)), `Dependencies` (`ITypeDependency.Target`, `TargetGenericArguments`), `Visibility`, `IsRecord`, `IsSealed`, `IsAbstract`, `AttributeInstances`, `GenericParameters`. Extension methods in `ArchUnitNET.Domain.Extensions` (`IsAssignableTo(string fullName)`, `ImplementsInterface(...)`, `GetFieldMembers()`, `GetMethodMembers()`, `GetPropertyMembers()`, `HasDependency`, …). Generic full names look like `DomainCentric.BuildingBlocks.Ddd.Tactical.IAggregateRoot`2` — prefer the non-generic base interfaces for assignability.

## Rule shape and ids

Each Java rule becomes one `IDcaRule` **with the same id, title and rationale** (copy them from `RULES.md`; adapt only language-specific words: "package" → "namespace", "AggregateRoot<T, ID>" → "IAggregateRoot", "records" stay (C# has records), "@Service"/Spring/JPA → see n/a rules, "final" → "sealed"/"readonly", "getter/setter" → "property setter", "Impl" fine). Keep the doctrine wording otherwise.

**Semantics must match the Java rule** — read the Java implementation, not only the title. Translate Java-isms:
- field checks → fields **and** auto-property backing (`PropertyMember`); "public setter" → property with public/internal `set` (an `init` accessor is not a setter — check via the setter method name `set_X` and its `IsInitOnly`-ish: ArchUnitNET exposes no init flag, so use reflection `PropertyInfo.SetMethod.ReturnParameter.GetRequiredCustomModifiers()` contains `IsExternalInit` when needed) or method `SetXxx(...)`.
- "final class" → `sealed` class **or** record/record struct; "final fields" → `readonly` fields / get-only or init-only properties.
- "record" → `Class.IsRecord == true` or a struct (`record struct` is a struct — ArchUnitNET `Structs`); attribute equality for hand-written classes → overrides `Equals` and `GetHashCode` (reflection).
- `Optional<T>` return → `Task<T?>`; repository/store methods are `*Async` returning `Task`/`Task<T>` — unwrap `Task<T>` generic arguments when checking return types (`MethodMember.ReturnTypeInstance.GenericArguments`).
- Java `List/Set/Collection<T>` → `IEnumerable<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, `List<T>`, `ICollection<T>`, `IList<T>`, arrays.
- `interface UseCase.execute(I)` → `IUseCase<TIn,TOut>.ExecuteAsync(TIn, CancellationToken)`.
- Inner/nested classes: `IType.IsNested`.

**n/a rules** — a Java rule whose only subject is a Spring/JPA/Modulith annotation and that has no meaningful .NET reading (e.g. "use cases must be annotated @Service", "package-info must carry @ApplicationModule") is **not ported**. Do not invent a substitute check. Instead list the id in the set's `NotApplicable` static (`public static readonly IReadOnlyDictionary<string,string> NotApplicable` = id → one-line reason) so `porting-status.md` and the catalog can show it. Rules about `@Transactional` map to `FrameworkTypes.TransactionScope` usage where the Java rule is about *where* transactions are demarcated; rules about `@RestController`/`@Controller` map to "derives from `ControllerBase`/`PageModel` or has `[ApiController]`" (by full name via `layout.FrameworkTypes`). Rules about "no Spring/JPA annotations in the domain" become "domain types carry no attributes from outside `System`/the building blocks" (use `ThirdPartyNamespacesAllowedInDomain`).

**Diagnostic rules** (title starts with `Diagnostic:`, Java body `arch -> {}`) stay as no-op `DcaRule.Check` with the same id — they carry doctrine into the catalog.

**Rule ids and order**: exactly the Java ids, in Java order, gaps where a rule is n/a (the n/a ids do **not** appear in `Rules`). `Rules` is built once in the constructor as an immutable list.

**No hard-coded namespaces from any sample** — everything comes from `DcaLayout` / `DcaArchitecture`.

**Rationale texts flow verbatim into a public knowledge catalog**: no references to ADRs (`ADR-0xx`), no sample repository paths, no "Claude"/plugin mentions.

## Fixtures and self-tests

Test project: `tests/DomainCentric.ArchRules.Tests/` (xUnit 2.9, `net10.0`, references ArchRules + BuildingBlocks; `ImplicitUsings` is off — write your `using`s). Every rule set gets its own fixture tree:

```
tests/DomainCentric.ArchRules.Tests/Fixtures/<Set>/Good/        ← root namespace DomainCentric.ArchRules.Tests.Fixtures.<Set>.Good
  SharedKernel/SharedKernelContext.cs        [SharedKernel] public static class …  (marker class, exact namespace …Good.SharedKernel)
  SharedKernel/Domain/Model/…                shared value objects if needed
  <Ctx>/<Ctx>Context.cs                      [BoundedContext("<Ctx>", Description = "…")] public static class <Ctx>Context {}
  <Ctx>/Domain/Model/…                       aggregate, entity, value (record), id (readonly record struct : IId), domain event
  <Ctx>/Application/<UseCase>/…              I*InputPort : IUseCase<Cmd,Result>, *UseCase : I*InputPort, *Command/*Query, *Result
  <Ctx>/Application/Shared/…                 I*Repository : IRepository<Agg, AggId>
  <Ctx>/Adapter/Incoming/…  <Ctx>/Adapter/Outgoing/…
tests/DomainCentric.ArchRules.Tests/Fixtures/<Set>/Bad/         ← same shape, root …<Set>.Bad, one violation per rule
```

Use file-scoped namespaces that spell the folder path exactly — the rules match on namespace names, not folders. Fixture classes implement the real building blocks. **No web framework dependency**: where a rule needs a framework type (controller base, `[ApiController]`, `TransactionScope`), declare a shim with the **same full name** the layout expects under `tests/DomainCentric.ArchRules.Tests/Shims/` (`namespace Microsoft.AspNetCore.Mvc; public abstract class ControllerBase {}` etc.). Only the **Hexagonal/Layered/Onion** agent creates shims; everyone else uses them (check the folder first; if missing, create only what you need and say so in your report).

Self-test class `tests/DomainCentric.ArchRules.Tests/Rules/<Set>RulesTests.cs`:

```csharp
private const string Good = "DomainCentric.ArchRules.Tests.Fixtures.<Set>.Good";
private const string Bad  = "DomainCentric.ArchRules.Tests.Fixtures.<Set>.Bad";
private static readonly IReadOnlyDictionary<string, string> NoNegativeFixture = new Dictionary<string, string>(); // id → reason

private static DcaArchitecture Arch(string ns) =>
    DcaArchitecture.Load(DcaLayout.ForRootNamespace(ns), typeof(<Set>RulesTests).Assembly);   // loads only that namespace

[Fact] public void RuleSetHasStableShape()   // Name, count, ids in order (skipping n/a ids), NotApplicable ids are not in Rules
[Theory][MemberData(nameof(RuleIds))] public void GoodFixturePasses(string id)   // every rule passes on Good
[Theory][MemberData(nameof(NegativeRuleIds))] public void BadFixtureFails(string id) // Assert.Throws<DcaRuleViolationException>, message not blank
```

Every rule needs a passing case. Every rule should have a failing case; where a negative fixture is impossible or absurd, list the id in `NoNegativeFixture` with a one-line reason. Fixtures deliberately violate style — the test csproj already suppresses the usual analyzer noise; add specific `#pragma warning disable` inside a fixture file if a warning-as-error blocks you, never loosen the csproj globally.

## Compile / test

**Tests run in Debug configuration** (the default). Verified: in Release builds the compiler emits async
state machines as structs and ArchUnitNET drops them, so a dependency that occurs only inside an
`async` method body is invisible to every dependency rule. In Debug the state machine is a class and its
dependencies are attributed to the declaring type. `DcaArchitecture.Load` refuses optimized assemblies
(opt-out `allowOptimizedAssemblies: true`). Fixtures may therefore put violations inside async bodies
— but keep at least one violation per rule in a synchronous member or field, so the Bad fixture also
fails for consumers who opt out.

```
export PATH=$HOME/.dotnet:$PATH DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
cd dca-dotnet && dotnet build && dotnet test --no-build --filter "FullyQualifiedName~<Set>RulesTests"
```

Other rule sets are stubs while the port is in flight — a red build from *another* agent's file is not yours to fix; report it. Do not edit files outside your rule-set class, its fixtures, its test class and `PORTING-LOG.md` (append-only, one `## <Set>` section per agent: rules ported, rules n/a with reason, semantic deviations, rules without negative fixture).
