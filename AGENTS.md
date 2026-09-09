# AGENTS.md

Guidance for AI coding agents working in `dca-dotnet` — the .NET twin of `dca-java`: `DomainCentric.BuildingBlocks`,
`DomainCentric.ArchRules` (ArchUnitNET, same rule ids as `dca-archunit`) and `DomainCentric.ArchRules.Xunit`.
`README.md` explains the packages, `RELEASING.md` the NuGet release, `PORTING.md` / `PORTING-LOG.md` the mapping
from Java, rule by rule.

## Principles that apply in every DCA repository

These hold for anyone working in any of the DCA repositories — human or agent — regardless of local tooling or memory.

1. **The samples exist to make the AI harness deterministic, not to ship features.** They are the experiment field
   for the harness: knowledge catalog, architecture rules, markers, plugins. Every architectural change in a sample
   answers three questions before it is done — does the catalog need a node (pitfall / decision / recipe /
   template)? could an ArchUnit / ArchUnitNET rule check it (same rule id in `dca-java` and `dca-dotnet`)? would one
   more *generic* marker in the building blocks make it checkable? Record the answer, "none" included, in the WP or
   ADR. Two agents building the same thing differently in the two samples is a determinism gap to close at the
   source (catalog, rule, marker), not with a code fix.
2. **Rules, markers and the catalog are general.** They are the foundation other production systems — any industry —
   build on with AI. Nothing in them may exist only because the e-commerce sample needs it: no shop vocabulary in
   rule or marker names or texts, no selection that only matches the sample's layout, no catalog node that presupposes
   a cart. The sample proves the general artifact; it is never its source of names or shapes.
3. **The core libraries are framework-neutral.** `dca-building-blocks` / `DomainCentric.BuildingBlocks` have zero
   dependencies; `dca-archunit` / `DomainCentric.ArchRules` reference frameworks only as configurable presets
   (`FrameworkAnnotations`, `FrameworkTypes`) and never on their class path. Framework-specific code goes into
   satellite artifacts (`dca-spring`, `dca-archunit-spring-modulith`) or into the sample. Spring is the default
   preset and the reference implementation's framework, not the vocabulary of the rules.
4. **Each reader artifact stands alone.** Guide, book and catalog bundle are independently readable; links never
   cross repository boundaries (the sample may cite the guide). `AGENTS.md` files are exempt — they carry the
   cross-project pointers and the sync duties.

## Working here

- **Build:** `dotnet build` / `dotnet test` (Debug — ArchUnitNET drops optimized async state machines);
  `dotnet run --project tools/RulesCatalog -- .` regenerates `rules.json` / `RULES.md`, which the knowledge catalog
  merges. Commit them with every rule change.
- **Same ids as Java.** A rule exists here with the id it has in `dca-archunit`, or it is listed in the set's
  `NotApplicable` with the reason. .NET-only rules are `DCA-NET-nnn`. Rule texts (`Selects`, `Checks`) use the same
  wording as Java wherever the reading is the same, and are framework- and domain-neutral.
- **`I`-prefixed markers, async ports, attributes instead of `package-info`, context marker class** — the
  conventions are in `src/DomainCentric.BuildingBlocks/README.md`; keep the Java ↔ C# table there current.
- **Frameworks only as presets** (`FrameworkTypes.AspNetCore()`, `FrameworkTypes.None()`); nothing from ASP.NET
  Core on the libraries' class path. `FrameworkNeutralityTests` fails the build when a rule text, an n/a reason or a
  building-block XML doc names a framework or the shop outside a "for example" sentence.
- **Versions** live in the `.csproj` `<Version>` elements (never `-p:Version`); `DomainCentric.BuildingBlocks` keeps
  the last released version because it is the ArchRules package dependency.
- **Consumers to keep in sync** (monorepo checkout): `dca-ecommerce-sample-dotnet`, the bootstrap skill in
  `dca-marketplace`, the knowledge catalog (regenerate), `planning/porting-status.md`.

WP-34 policy: use-case stereotypes are optional; configuration registration is equally valid.
NAM-002 is a non-failing Java diagnostic, not a wiring guarantee. Outgoing adapters may
reuse global/own infrastructure. Domain metadata rules classify configured roles on
types and members (including composed metadata), allow unclassified metadata, and assign
exclusive ownership to ADV-004/011/015/018 before ONI-003.

WP-35: USE-016 forbids direct/input-port/helper-mediated operation invocation except
explicit caller-side coordinator exclusions; CYC-005 checks operation slices even within
one feature. USE-017 maps the effective public surface to input ports (inherited/explicit
implementations valid, unrelated methods/properties forbidden). MAP-008 requires a
translation site for each declared upstream/channel; shared adapter packages are allowed.

WP-36 policy (2026-09-09): integration contracts use the configured events segment only; translators use adapter/outgoing/event. Events are optional with conservative USE-009 proof. USE-012 exists in both libraries; static boundary evidence is not runtime containment. Delivery is per consumer/effect with snapshot replay, bounded retry and explicit manual recovery; never claim local keys alone prevent external duplicates.
