using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using DomainCentric.BuildingBlocks.Application;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DomainCentric.ArchRules.Rules;

/// <summary>
/// Error-handling rules: a failure carries the name of what went wrong, in the layer that knows it.
/// </summary>
/// <remarks>
/// Three layers, three kinds of failure. A broken rule of the model is a <see cref="DomainException"/> in the
/// domain layer; a failure a port's contract declares is a <see cref="PortException"/> in the application layer -
/// a request the application cannot serve a <see cref="UseCaseException"/> (an input port's failure), a conflict an
/// output port reports an <see cref="OutputPortException"/>; the translation into a protocol answer happens in the incoming adapter, which is the only layer that
/// knows the protocol. Argument guards are outside all of this — a null or range check states a contract for
/// the caller and keeps the platform's own argument exception. Whether a specific <c>throw</c> should have been
/// a domain exception is not decidable from the type model; that judgement stays with review.
/// </remarks>
public sealed class ErrorHandlingRules : IDcaRuleSet
{
    /// <summary>Java rules of this set that have no .NET reading (none).</summary>
    public static readonly IReadOnlyDictionary<string, string> NotApplicable = new Dictionary<string, string>();

    private static readonly string[] ForbiddenSuffixes = { "Error", "Fault", "Failure" };

    private static readonly string[] ForbiddenWords = { "Http", "StatusCode", "ResponseStatus", "ResponseEntity" };

    public ErrorHandlingRules(DcaLayout layout)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Rules = new IDcaRule[]
        {
            DomainExceptionsExtendDomainException(),
            ExceptionsResideInTheirLayer(),
            ApplicationExceptionsExtendUseCaseException(),
            ExceptionsCarryNoFrameworkMetadata(),
            ExceptionsNameNoTransportConcept(),
            AdaptersWithoutTranslation(),
            FailuresCarryIdsAndValues(),
            ClosedResultsRefuse(),
            DomainExceptionsConstructedInTheDomain(),
        };
    }

    public string Name => "errors";

    public IReadOnlyList<IDcaRule> Rules { get; }

    /// <summary>The layout this rule set was built for.</summary>
    public DcaLayout Layout { get; }

    public IDcaRule DomainExceptionsExtendDomainException() =>
        DcaRule.Check(
            "DCA-ERR-001",
            "Exceptions declared in the domain layer must extend DomainException",
            "A failure the model raises is a broken business rule and says so through its type; a caller"
                + " that cannot tell one from the platform's own failures cannot answer either of them"
                + " properly",
            arch =>
            {
                var violations = ProjectExceptions(arch)
                    .Where(arch.InDomain)
                    .Where(t => !arch.Layout.Markers.Is(nameof(DcaMarkers.DomainException), t))
                    .Select(t => $"{t.FullName} does not extend DomainException")
                    .ToList();
                DcaRule.Fail("A domain failure without the domain base type", violations);
            })
            .Selecting(
                "Types under <Module>.Domain of every module root that are assignable to Exception - the "
                + "exception types the domain layer declares itself. The base type DomainException lives in "
                + "the building blocks and is not selected.")
            .Checking(
                "Each derives from DomainException, directly or through an intermediate base class. Where "
                + "the exception is thrown is not checked - a throw site is not part of the type model - so "
                + "a guard that raises the platform's argument exception is outside this rule. An empty "
                + "selection passes.");

    public IDcaRule ExceptionsResideInTheirLayer() =>
        DcaRule.Check(
            "DCA-ERR-002",
            "Domain and port exceptions reside in the layer whose failure they name",
            "The base type states which layer owns the failure; declaring it elsewhere puts the vocabulary"
                + " of that layer outside it and lets an adapter invent failures the application never"
                + " reports",
            arch =>
            {
                var violations = new List<string>();
                foreach (var type in ProjectExceptions(arch))
                {
                    if (arch.Layout.Markers.Is(nameof(DcaMarkers.DomainException), type) && !arch.InDomain(type))
                    {
                        violations.Add($"{type.FullName} is a domain exception outside the domain layer");
                    }

                    if (arch.Layout.Markers.Is(nameof(DcaMarkers.PortException), type) && !arch.InApplication(type))
                    {
                        violations.Add($"{type.FullName} is a port exception - use-case or output-port - outside the application layer");
                    }
                }

                DcaRule.Fail("An exception resides outside the layer its base type names", violations);
            })
            .Selecting(
                "Types anywhere under the root namespace that are assignable to DomainException or to "
                + "PortException, except the base types themselves and anything else in the "
                + "building-blocks namespace.")
            .Checking(
                "A subtype of DomainException resides under <Module>.Domain of some module root, a subtype "
                + "of PortException - UseCaseException, OutputPortException and any other port failure - under "
                + "<Module>.Application. An output port's failure is declared beside the port, even though the "
                + "adapter that implements the port raises it. Both findings are collected into one "
                + "violation. Which of the two a given failure should have been is not checked here. An "
                + "empty selection passes.");

    public IDcaRule ApplicationExceptionsExtendUseCaseException() =>
        DcaRule.Check(
            "DCA-ERR-003",
            "Exceptions declared in the application layer must extend UseCaseException or OutputPortException",
            "A use case that cannot serve a request reports why through its type, so that one incoming"
                + " adapter can map each outcome onto its protocol and another adapter can map the same"
                + " outcome onto a different one; an output port that declares a failure in its contract names it"
                + " the same way, so the use case can catch exactly that outcome",
            arch =>
            {
                var violations = ProjectExceptions(arch)
                    .Where(arch.InApplication)
                    // A subtype of DomainException here is misplaced, not mis-based: DCA-ERR-002 owns it
                    // and says to move it to the domain. This rule's remedy would turn a broken rule of
                    // the model into a use-case failure.
                    .Where(t => !arch.Layout.Markers.Is(nameof(DcaMarkers.DomainException), t))
                    .Where(t => !arch.Layout.Markers.Is(nameof(DcaMarkers.UseCaseException), t))
                    .Where(t => !arch.Layout.Markers.Is(nameof(DcaMarkers.OutputPortException), t))
                    .Select(t => $"{t.FullName} extends neither UseCaseException nor OutputPortException")
                    .ToList();
                DcaRule.Fail("A use-case failure without the application base type", violations);
            })
            .Selecting(
                "Types under <Module>.Application of every module root that are assignable to Exception and "
                + "not to DomainException - the exception types the application layer declares itself. The "
                + "base types live in the building blocks and are not selected. A subtype of "
                + "DomainException declared in an application namespace is not selected either: it is "
                + "misplaced rather than mis-based, and DCA-ERR-002 owns that case with the remedy that fits "
                + "it - move the failure to the domain, do not change its base type.")
            .Checking(
                "Each derives from UseCaseException - a failure an input port declares, raised by the use case - "
                + "or OutputPortException - a failure an output port declares, raised by its adapter - directly "
                + "or through an intermediate base class. Deriving from PortException or InputPortException "
                + "directly does not count: they classify, they are not derived from by a project. An empty "
                + "selection passes.");

    public IDcaRule ExceptionsCarryNoFrameworkMetadata() =>
        DcaRule.Check(
            "DCA-ERR-004",
            "Domain and port exceptions must not carry prohibited framework metadata",
            "An exception of an inner layer that carries container, persistence or protocol metadata has"
                + " decided how the outside answers it, which is the incoming adapter's decision and only"
                + " its; an attribute that fixes the answer's status decides it for every protocol at once,"
                + " including the ones the exception knows nothing about",
            arch =>
            {
                var roles = arch.Layout.FrameworkTypes;
                var violations = new List<string>();
                var unreadable = new List<string>();
                foreach (var type in ExceptionsOfEitherLayer(arch))
                {
                    var runtime = arch.RuntimeType(type);
                    if (runtime is null)
                    {
                        // Not a violation, but not a clean pass either: say so rather than skip silently.
                        unreadable.Add(type.FullName);
                        continue;
                    }

                    foreach (var attribute in runtime.GetCustomAttributesData())
                    {
                        for (var attributeType = attribute.AttributeType; attributeType is not null; attributeType = attributeType.BaseType)
                        {
                            var classified = roles.PersistenceAttributeTypes.Contains(attributeType.FullName ?? "")
                                || roles.TransportStatusAttributeTypes.Contains(attributeType.FullName ?? "")
                                || roles.ContainerAttributeNamespaces
                                    .Concat(roles.PersistenceAttributeNamespaces)
                                    .Concat(roles.TransactionAttributeNamespaces)
                                    .Concat(roles.WebControllerAttributeNamespaces)
                                    .Concat(roles.RestControllerAttributeNamespaces)
                                    .Concat(roles.EventListenerAttributeNamespaces)
                                    .Any(prefix => DcaLayout.IsBelow(attributeType.Namespace ?? "", prefix));
                            if (!classified)
                            {
                                continue;
                            }

                            violations.Add($"{type.FullName} carries prohibited metadata {attribute.AttributeType.FullName}");
                            break;
                        }
                    }
                }

                if (unreadable.Count > 0)
                {
                    Console.Out.WriteLine(
                        $"[DCA-ERR-004] {unreadable.Count} exception type(s) not inspected, runtime type unavailable: "
                        + string.Join(", ", unreadable));
                }

                DcaRule.Fail("Prohibited metadata on an inner-layer exception", violations);
            })
            .Selecting(
                "Types anywhere under the root namespace that are assignable to the domain-exception or the "
                + "port-exception role - UseCaseException and OutputPortException included - the vocabulary's own "
                + "code excluded: the namespaces the configured "
                + "role types live in are not selected, so a project's own base type is not reported as "
                + "residing outside a layer it never claimed.")
            .Checking(
                "The type carries no attribute the layout classifies into the container, persistence, "
                + "transaction, transport-status, web-controller, REST-controller or event-listener role, "
                + "neither directly nor through a base attribute type - the same seven roles the Java twin "
                + "forbids. Four of them are empty in every preset: the platform has no container "
                + "stereotype, no controller attribute a failure could carry and no event-listener "
                + "attribute, and it answers a failure through a mapper type rather than through an "
                + "attribute on it. They exist so the id has one contract in both languages and a project "
                + "whose framework does have such an attribute names it. Fields, properties and methods "
                + "are not inspected, and an attribute the layout classifies into no role is allowed. A "
                + "type whose runtime reflection is unavailable is not inspected; it is counted and named "
                + "in the rule's own outcome rather than passing silently. With the roles empty the rule selects "
                + "no metadata and passes.");

    public IDcaRule ExceptionsNameNoTransportConcept() =>
        DcaRule.Check(
            "DCA-ERR-005",
            "Domain and port exception names must stay in the language of their layer",
            "A failure named after a transport concept is a decision about the answer, taken in a layer"
                + " that does not know the protocol; the name should say what went wrong, not what the"
                + " caller should be told",
            arch =>
            {
                var violations = new List<string>();
                foreach (var type in ExceptionsOfEitherLayer(arch))
                {
                    foreach (var suffix in ForbiddenSuffixes.Where(s => type.Name.EndsWith(s, StringComparison.Ordinal)))
                    {
                        violations.Add($"{type.FullName} ends with the technical suffix {suffix}");
                    }

                    foreach (var word in ForbiddenWords.Where(w => type.Name.Contains(w, StringComparison.Ordinal)))
                    {
                        violations.Add($"{type.FullName} names the transport concept {word}");
                    }
                }

                DcaRule.Fail("A technical or transport word in an exception name", violations);
            })
            .Selecting(
                "Types anywhere under the root namespace that are assignable to the domain-exception or the "
                + "port-exception role - UseCaseException and OutputPortException included - the vocabulary's own "
                + "code excluded: the namespaces the configured "
                + "role types live in are not selected, so a project's own base type is not reported as "
                + "residing outside a layer it never claimed.")
            .Checking(
                "The type name ends with none of Error, Fault, Failure and contains none of Http, "
                + "StatusCode, ResponseStatus, ResponseEntity. The bare words Status and Response are "
                + "not matched: they are transport words and domain words, and the name alone does not "
                + "tell them apart, so OrderStatusInvalidException passes. Whether the remaining name "
                + "is a term of the Ubiquitous Language is not decidable here and stays with review. "
                + "An empty selection passes.");

    public IDcaRule AdaptersWithoutTranslation() =>
        DcaRule.Informational(
            "DCA-ERR-006",
            "Diagnostic: incoming adapter namespaces that drive a use case without translating its failures",
            "A namespace that drives the application and knows neither failure type nor a case of a closed"
                + " result either lets everything escape to a generic handler or catches a generic type and answers"
                + " every outcome the same way; whether it does is not visible in the type model. A use case may"
                + " return its expected refusals as cases of a closed result instead of throwing them; a switch"
                + " over those cases is the translation then. An adapter that catches the abstract base of the"
                + " port failures answers every outcome the same way",
            arch =>
            {
                var domainException = arch.Layout.Markers.DomainException;
                var useCaseException = arch.Layout.Markers.UseCaseException;
                var anyFailureType = arch.Types.Any(t =>
                    t.FullName != domainException && t.FullName != useCaseException
                    && (arch.Layout.Markers.Is(nameof(DcaMarkers.DomainException), t) || arch.Layout.Markers.Is(nameof(DcaMarkers.UseCaseException), t)));
                if (!anyFailureType)
                {
                    return new List<string>
                    {
                        $"no class of the scan is assignable to {domainException} or {useCaseException}, so no namespace "
                        + "can be seen to name a failure - nothing is listed. Where the project has failure types of its "
                        + "own, point the DomainException and UseCaseException roles at their base types "
                        + "(DcaMarkers with-expression).",
                    };
                }

                var observed = GenericPortFailureCatches(arch);
                var resultCases = CasesOfClosedResults(arch);
                var driving = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
                var namespacesThatName = new HashSet<string>(StringComparer.Ordinal);
                foreach (var type in arch.Types)
                {
                    var ns = type.Namespace?.FullName ?? "";
                    if (!arch.InIncomingAdapter(type))
                    {
                        continue;
                    }

                    var targets = type.Dependencies.Select(d => d.Target).ToList();
                    if (targets.Any(t => arch.Layout.Markers.Is(nameof(DcaMarkers.DomainException), t) || arch.Layout.Markers.Is(nameof(DcaMarkers.UseCaseException), t)
                        || resultCases.Contains(t.FullName)))
                    {
                        namespacesThatName.Add(ns);
                    }

                    if (type is not Interface && targets.Any(t => arch.Layout.Markers.Is(nameof(DcaMarkers.InputPort), t)))
                    {
                        if (!driving.TryGetValue(ns, out var names))
                        {
                            names = new List<string>();
                            driving[ns] = names;
                        }

                        names.Add(type.Name);
                    }
                }

                foreach (var (ns, names) in driving)
                {
                    if (namespacesThatName.Contains(ns))
                    {
                        continue;
                    }

                    names.Sort(StringComparer.Ordinal);
                    observed.Add(
                        $"{ns}: drives an input port and names neither a failure type of the inner layers nor a case of a closed result ({string.Join(", ", names)})");
                }

                return observed;
            })
            .Selecting(
                "Incoming adapter namespaces of every module root (<Module>.Adapter.Incoming) that hold at "
                + "least one non-interface type depending on a type assignable to IInputPort - the "
                + "namespaces that drive the application.")
            .Checking(
                "Informational diagnostic only: when no type of the scan is assignable to DomainException or "
                + "UseCaseException, it lists no namespace and says once that it cannot see the failures; otherwise it "
                + "lists a namespace when no type in it - a central exception "
                + "handler beside the adapters included - depends on a type assignable to DomainException or "
                + "UseCaseException, nor on a case of a closed result, and never fails. A closed result is a type "
                + "an input port answers with - a type argument of its use-case interface or the return type of "
                + "one of its methods, a task unwrapped - that derives from one of the configured closed-result "
                + "base types (FrameworkTypes.ClosedResultBaseTypes, matched by name; every preset names the "
                + "generic base of a discriminated-union library, for example OneOf.OneOfBase); its cases are the "
                + "generic arguments of that base. A type test, a cast, a case type in a function's or an action's "
                + "signature (one handler per case) and a member call on the union base that names its cases all "
                + "count, so this approximates an exhaustive switch: one reference to one case counts, and whether "
                + "every case is handled is left to the compiler. A handler in a different namespace of the same "
                + "module is not seen, and a caught type is not visible to the type model, so this does not "
                + "establish that a namespace translates nothing. Beside the namespaces it lists every incoming-"
                + "adapter type that catches - read from the exception-handling clauses of its methods' bodies, "
                + "compiler-generated nested types such as async state machines included - or refers to the "
                + "abstract bases PortException, InputPortException or OutputPortException themselves: it handles "
                + "the port failures generically.");

    /// <summary>
    /// DCA-ERR-007 (experimental, informational). A failure carries ids and figures, never what a page shows. A
    /// plain string field may hold a code, so the rule observes rather than fails; the remedy is a typed value.
    /// </summary>
    public IDcaRule FailuresCarryIdsAndValues() =>
        DcaRule.Informational(
            "DCA-ERR-007",
            "Diagnostic: failures that carry display data or objects instead of ids and figures",
            "A failure carries ids and figures, never what a page shows: its fields are the typed identities"
                + " and the values the rule compared. A name or a description of the thing the id refers to is"
                + " resolved by the translation site, through the queries it already has; a failure that"
                + " carries it decides for every surface what it shows, and one that carries the aggregate"
                + " hands its state to every caller",
            arch =>
            {
                var markers = arch.Layout.Markers;
                var observed = new List<string>();
                foreach (var exception in ExceptionsOfEitherLayer(arch).OrderBy(t => t.FullName, StringComparer.Ordinal))
                {
                    var runtime = arch.RuntimeType(exception);
                    for (var type = runtime; type is not null && !DcaMarkers.IsRole(type, markers.DomainException)
                        && !DcaMarkers.IsRole(type, markers.UseCaseException) && !DcaMarkers.IsRole(type, markers.OutputPortException)
                        && !DcaMarkers.IsRole(type, markers.PortException) && type != typeof(Exception); type = type.BaseType)
                    {
                        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                        {
                            var finding = FieldFinding(field.FieldType, markers);
                            if (finding is not null)
                            {
                                observed.Add($"{exception.FullName} holds {FieldName(field)} as {finding} - carry the id or the value the rule compared");
                            }
                        }
                    }
                }

                return observed.Distinct().ToList();
            })
            .Experimental()
            .Selecting(
                "The project's own types below DomainException or PortException, with their runtime type in"
                + " the loaded assemblies, and the instance fields they and their project base types declare -"
                + " the backing fields of auto-properties included.")
            .Checking(
                "Informational diagnostic only: lists a field whose type is a plain string, or is assignable to"
                + " IAggregateRoot or IEntity - directly, as an array element or as a generic type argument -"
                + " and never fails. Ids, values, numbers and every other type pass. A string that holds a code"
                + " rather than display text is listed as well: the type does not tell the two apart, and the"
                + " remedy is a typed value for the code.");

    /// <summary>
    /// DCA-ERR-008 (experimental, informational). Lists every closed result an input port answers with that has no
    /// case marked as a refusal. A closed result may hold only outcomes that are not refusals; whether a case refuses
    /// is domain knowledge the type does not show, so the rule asks rather than fails.
    /// </summary>
    public IDcaRule ClosedResultsRefuse() =>
        DcaRule.Informational(
            "DCA-ERR-008",
            "Diagnostic: closed use-case results without a case marked as a refusal",
            "A closed result describes the outcomes a use case can answer with; some of them are refusals, some are"
                + " not. A result may hold several outcomes of which none refuses - found or nothing found, sent or"
                + " already sent - and that alone is reason enough to close it. The refusal marker on a case says the"
                + " request was refused, a meaning the caller answers with or without a transaction, and it is what"
                + " lets the transaction boundary roll a refusal back like a thrown failure. Whether a case refuses is"
                + " domain knowledge the type does not show, so a closed result without any marked case is listed for"
                + " a look, not failed: if one of its outcomes is a refusal and stays unmarked, the transaction"
                + " commits the writes made before it",
            arch =>
            {
                var markers = arch.Layout.Markers;
                var observed = new List<string>();
                foreach (var (result, closed) in ClosedResults(arch))
                {
                    if (!arch.InLayer(result, DcaLayout.Layer.Application))
                    {
                        continue;
                    }

                    if (closed.Cases.Any(c => markers.Is(nameof(DcaMarkers.Refusal), c)))
                    {
                        continue;
                    }

                    observed.Add(
                        $"{result.FullName} is answered by [{string.Join(", ", closed.Ports.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal))}]"
                        + $" and none of its cases [{string.Join(", ", closed.Cases.Select(c => c.Name).OrderBy(n => n, StringComparer.Ordinal))}]"
                        + $" is marked as a refusal ({markers.Marker(nameof(DcaMarkers.Refusal))}) - check: is none of these"
                        + " outcomes a refusal? If one is, mark it, or the transaction commits when it is returned");
                }

                return observed;
            })
            .Experimental()
            .Selecting(
                "Closed results under <Module>.Application of every module root: a type an input port answers with"
                + " - a type argument of its use-case interface or the return type of one of its methods, a task"
                + " unwrapped - that derives from one of the configured closed-result base types"
                + " (FrameworkTypes.ClosedResultBaseTypes, matched by name). An empty list of base types selects"
                + " nothing.")
            .Checking(
                "Informational diagnostic only: lists every closed result none of whose cases - the generic arguments"
                + " of the closed-result base - is assignable to the configured refusal role (by default the IRefusal"
                + " marker of the building blocks), and never fails. The result type itself carrying the marker does"
                + " not count. A listed result is not a finding by itself: a closed result whose outcomes are all"
                + " successes - found or nothing found - is correct as it is. A context that throws its refusals has"
                + " no closed results and lists nothing.");

    /// <summary>
    /// DCA-ERR-009 (experimental). A domain exception is constructed only in the domain layer: the model refuses,
    /// a use case never invents a domain failure.
    /// </summary>
    public IDcaRule DomainExceptionsConstructedInTheDomain() =>
        DcaRule.Check(
            "DCA-ERR-009",
            "A domain exception is constructed only in the domain layer",
            "The model refuses: a domain exception names a business rule the model itself broke off. A use case never"
                + " invents one. A failure the use case finds - an output port returned nothing, the caller does not own"
                + " what it names, a precondition on a second aggregate - is the application's, a case of its result or a"
                + " use-case exception; dressed as a domain failure it claims a rule the model never checked, and the"
                + " model cannot know what a search found",
            arch =>
            {
                var markers = arch.Layout.Markers;
                var vocabulary = markers.DeclaringNamespacesOf(new[] { nameof(DcaMarkers.DomainException) });
                var violations = new SortedSet<string>(StringComparer.Ordinal);
                foreach (var origin in arch.RuntimeTypes().Where(t => !t.IsNested))
                {
                    var ns = origin.Namespace ?? "";
                    if (arch.InLayer(origin, DcaLayout.Layer.Domain) || vocabulary.Any(v => DcaLayout.IsBelow(ns, v)))
                    {
                        continue;
                    }

                    foreach (var unit in new IntraClassCalls(origin).Units)
                    {
                        foreach (var (code, operand) in IntraClassCalls.IlInstructions(unit))
                        {
                            if (code != System.Reflection.Emit.OpCodes.Newobj || operand is not ConstructorInfo constructor
                                || !markers.Is(nameof(DcaMarkers.DomainException), constructor.DeclaringType))
                            {
                                continue;
                            }

                            violations.Add(
                                $"{origin.FullName}.{IntraClassCalls.DisplayName(unit)} constructs {constructor.DeclaringType!.FullName}"
                                + " - only the domain layer raises a domain failure; a failure the application finds is a case of"
                                + " its result or a use-case exception");
                        }
                    }
                }

                DcaRule.Fail(
                    "A domain exception is constructed outside the domain layer",
                    violations,
                    "let the aggregate, value object or domain service that checks the rule throw it; for a failure the use"
                    + " case finds itself - nothing found, not owned - return a refusal case of the result or throw a"
                    + " use-case exception declared beside the use case");
            })
            .Experimental()
            .Selecting(
                "Every type of the scan outside <Module>.Domain of every module root - the application layer, the adapters,"
                + " configuration and any unplaced type - with each object creation its methods and constructors make,"
                + " the compiler's async state machines, lambda closures and nested types included. Types in the"
                + " namespace of the configured domain-exception role are library code and not selected; test"
                + " assemblies are not loaded.")
            .Checking(
                "No such creation (a newobj instruction) creates a type assignable to the configured domain-exception role"
                + " (by default DomainException of the building blocks). A constructor chaining to its base constructor"
                + " is not a creation - where a misplaced exception type lives is DCA-ERR-002's finding. Construction in"
                + " the domain layer passes, a domain service and a factory included. A domain exception the application"
                + " catches, wraps or rethrows is not constructed and passes. A construction through reflection or a"
                + " generic factory method outside the domain is not seen. An empty selection passes.");

    /// <summary>
    /// The full names of the cases of every closed result an input port answers with: a type argument of a
    /// generic interface the port extends, or a method's return type with tasks unwrapped, that derives from a
    /// configured closed-result base; its cases are that base's generic arguments.
    /// </summary>
    private static HashSet<string> CasesOfClosedResults(DcaArchitecture arch) =>
        new(
            ClosedResults(arch).Values.SelectMany(closed => closed.Cases).Select(c => c.FullName ?? c.Name),
            StringComparer.Ordinal);

    /// <summary>A closed result: the input ports that answer with it and its cases.</summary>
    private sealed record ClosedResult(SortedSet<Type> Ports, List<Type> Cases);

    /// <summary>
    /// Every closed result an input port answers with - a type argument of a generic interface the port extends,
    /// or a method's return type with tasks unwrapped, that derives from a configured closed-result base - with
    /// the ports that answer with it and its cases, that base's generic arguments.
    /// </summary>
    private static SortedDictionary<Type, ClosedResult> ClosedResults(DcaArchitecture arch)
    {
        var byName = Comparer<Type>.Create((a, b) => string.CompareOrdinal(a.FullName ?? a.Name, b.FullName ?? b.Name));
        var results = new SortedDictionary<Type, ClosedResult>(byName);
        var bases = new HashSet<string>(arch.Layout.FrameworkTypes.ClosedResultBaseTypes, StringComparer.Ordinal);
        if (bases.Count == 0)
        {
            return results;
        }

        foreach (var port in arch.RuntimeTypes())
        {
            if (!port.IsInterface || !arch.Layout.Markers.Is(nameof(DcaMarkers.InputPort), port))
            {
                continue;
            }

            var candidates = new List<Type>();
            foreach (var contract in port.GetInterfaces().Append(port))
            {
                if (contract.IsGenericType)
                {
                    candidates.AddRange(contract.GetGenericArguments());
                }

                candidates.AddRange(contract.GetMethods().Select(m => m.ReturnType));
            }

            foreach (var candidate in candidates.SelectMany(Unwrapped))
            {
                for (var current = candidate.BaseType; current is not null; current = current.BaseType)
                {
                    if (current.IsGenericType && current.FullName is not null
                        && bases.Contains(current.GetGenericTypeDefinition().FullName!.Split('`')[0]))
                    {
                        if (!results.TryGetValue(candidate, out var closed))
                        {
                            closed = new ClosedResult(new SortedSet<Type>(byName), new List<Type>());
                            results[candidate] = closed;
                        }

                        closed.Ports.Add(port);
                        foreach (var argument in current.GetGenericArguments().Where(a => !closed.Cases.Contains(a)))
                        {
                            closed.Cases.Add(argument);
                        }
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// The incoming-adapter types that catch, or depend on, one of the abstract port-failure bases themselves: a
    /// catch clause is read from the method bodies of the runtime type and its nested types (an async method's
    /// body lives in a compiler-generated state machine), a dependency from the type model.
    /// </summary>
    private static List<string> GenericPortFailureCatches(DcaArchitecture arch)
    {
        var markers = arch.Layout.Markers;
        var bases = new[] { markers.PortException, markers.InputPortException, markers.OutputPortException };
        var found = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var type in arch.Types.Where(arch.InIncomingAdapter))
        {
            foreach (var target in type.Dependencies.Select(d => d.Target.FullName).Where(bases.Contains))
            {
                found.Add($"{OuterName(type.FullName)}: names the abstract base {target[(target.LastIndexOf('.') + 1)..]} - catch the named failures, each with its own answer");
            }

            var runtime = arch.RuntimeType(type);
            if (runtime is null)
            {
                continue;
            }

            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            var bodies = WithNested(runtime).SelectMany(t => t.GetMethods(all).Cast<MethodBase>().Concat(t.GetConstructors(all)));
            foreach (var method in bodies)
            {
                IList<ExceptionHandlingClause> clauses;
                try
                {
                    clauses = method.GetMethodBody()?.ExceptionHandlingClauses ?? (IList<ExceptionHandlingClause>)Array.Empty<ExceptionHandlingClause>();
                }
                catch (Exception e) when (e is InvalidOperationException or NotSupportedException or BadImageFormatException)
                {
                    continue;
                }

                foreach (var clause in clauses)
                {
                    if (clause.Flags != ExceptionHandlingClauseOptions.Clause)
                    {
                        continue;
                    }

                    var caught = clause.CatchType?.FullName;
                    if (caught is not null && bases.Contains(caught))
                    {
                        found.Add($"{OuterName(runtime.FullName ?? type.FullName)}: names the abstract base {caught[(caught.LastIndexOf('.') + 1)..]} - catch the named failures, each with its own answer");
                    }
                }
            }
        }

        return found.ToList();
    }

    /// <summary>The type and every type nested in it, compiler-generated ones included, recursively.</summary>
    private static IEnumerable<Type> WithNested(Type type) =>
        new[] { type }.Concat(
            type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).SelectMany(WithNested));

    /// <summary>The outermost declaring type's name: a compiler-generated nested type reports its owner.</summary>
    private static string OuterName(string fullName) =>
        fullName.IndexOf('+', StringComparison.Ordinal) is var plus and > 0 ? fullName[..plus] : fullName;

    /// <summary>The type and, for a generic one such as a task, its generic arguments, recursively.</summary>
    private static IEnumerable<Type> Unwrapped(Type type)
    {
        yield return type;
        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            foreach (var inner in type.GetGenericArguments().SelectMany(Unwrapped))
            {
                yield return inner;
            }
        }
    }

    /// <summary>What is wrong with a failure's field type, worded for the line, or <c>null</c>.</summary>
    private static string? FieldFinding(Type type, DcaMarkers markers)
    {
        if (type == typeof(string))
        {
            return "a plain string";
        }

        if (markers.Is(nameof(DcaMarkers.AggregateRoot), type))
        {
            return $"the aggregate {type.Name}";
        }

        if (markers.Is(nameof(DcaMarkers.Entity), type))
        {
            return $"the entity {type.Name}";
        }

        var parts = type.IsArray ? new[] { type.GetElementType()! } : type.IsGenericType ? type.GetGenericArguments() : Type.EmptyTypes;
        return parts.Select(part => FieldFinding(part, markers)).FirstOrDefault(f => f is not null) is { } inner
            ? $"{type.Name.Split('`')[0]} of {inner}"
            : null;
    }

    /// <summary>A field's source name: the property name for an auto-property's backing field.</summary>
    private static string FieldName(FieldInfo field)
    {
        var name = field.Name;
        return name.StartsWith('<') && name.IndexOf('>') > 1 ? name[1..name.IndexOf('>')] : name;
    }

    /// <summary>The project's own exception types: assignable to Exception, outside the building blocks.</summary>
    private static IEnumerable<IType> ProjectExceptions(DcaArchitecture arch) =>
        arch.Types
            .Where(t => t is not Interface)
            .Where(t => !arch.Layout.Markers.DeclaresTypesIn(t.Namespace?.FullName ?? ""))
            .Where(t => IsAssignableTo(t, typeof(Exception).FullName!));

    /// <summary>The project's own types below the domain-exception or the port-exception base type.</summary>
    private static IEnumerable<IType> ExceptionsOfEitherLayer(DcaArchitecture arch) =>
        ProjectExceptions(arch)
            .Where(t => arch.Layout.Markers.Is(nameof(DcaMarkers.DomainException), t) || arch.Layout.Markers.Is(nameof(DcaMarkers.PortException), t));

    private static bool IsAssignableTo(IType type, string target) => type.IsAssignableTo(target);
}
