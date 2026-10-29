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
/// domain layer; a request the application cannot serve is a <see cref="UseCaseException"/> in the application
/// layer; the translation into a protocol answer happens in the incoming adapter, which is the only layer that
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
                    .Where(t => !IsAssignableTo(t, arch.Layout.Markers.DomainException))
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
            "Domain and use-case exceptions reside in the layer whose failure they name",
            "The base type states which layer owns the failure; declaring it elsewhere puts the vocabulary"
                + " of that layer outside it and lets an adapter invent failures the application never"
                + " reports",
            arch =>
            {
                var violations = new List<string>();
                foreach (var type in ProjectExceptions(arch))
                {
                    if (IsAssignableTo(type, arch.Layout.Markers.DomainException) && !arch.InDomain(type))
                    {
                        violations.Add($"{type.FullName} is a domain exception outside the domain layer");
                    }

                    if (IsAssignableTo(type, arch.Layout.Markers.UseCaseException) && !arch.InApplication(type))
                    {
                        violations.Add($"{type.FullName} is a use-case exception outside the application layer");
                    }
                }

                DcaRule.Fail("An exception resides outside the layer its base type names", violations);
            })
            .Selecting(
                "Types anywhere under the root namespace that are assignable to DomainException or to "
                + "UseCaseException, except the two base types themselves and anything else in the "
                + "building-blocks namespace.")
            .Checking(
                "A subtype of DomainException resides under <Module>.Domain of some module root, a subtype "
                + "of UseCaseException under <Module>.Application. Both findings are collected into one "
                + "violation. Which of the two a given failure should have been is not checked here. An "
                + "empty selection passes.");

    public IDcaRule ApplicationExceptionsExtendUseCaseException() =>
        DcaRule.Check(
            "DCA-ERR-003",
            "Exceptions declared in the application layer must extend UseCaseException",
            "A use case that cannot serve a request reports why through its type, so that one incoming"
                + " adapter can map each outcome onto its protocol and another adapter can map the same"
                + " outcome onto a different one",
            arch =>
            {
                var violations = ProjectExceptions(arch)
                    .Where(arch.InApplication)
                    // A subtype of DomainException here is misplaced, not mis-based: DCA-ERR-002 owns it
                    // and says to move it to the domain. This rule's remedy would turn a broken rule of
                    // the model into a use-case failure.
                    .Where(t => !IsAssignableTo(t, arch.Layout.Markers.DomainException))
                    .Where(t => !IsAssignableTo(t, arch.Layout.Markers.UseCaseException))
                    .Select(t => $"{t.FullName} does not extend UseCaseException")
                    .ToList();
                DcaRule.Fail("A use-case failure without the application base type", violations);
            })
            .Selecting(
                "Types under <Module>.Application of every module root that are assignable to Exception and "
                + "not to DomainException - the exception types the application layer declares itself. The "
                + "base type UseCaseException lives in the building blocks and is not selected. A subtype of "
                + "DomainException declared in an application namespace is not selected either: it is "
                + "misplaced rather than mis-based, and DCA-ERR-002 owns that case with the remedy that fits "
                + "it - move the failure to the domain, do not change its base type.")
            .Checking(
                "Each derives from UseCaseException, directly or through an intermediate base class. An "
                + "empty selection passes.");

    public IDcaRule ExceptionsCarryNoFrameworkMetadata() =>
        DcaRule.Check(
            "DCA-ERR-004",
            "Domain and use-case exceptions must not carry prohibited framework metadata",
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
                + "use-case-exception role, the vocabulary's own code excluded: the namespaces the configured "
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
            "Domain and use-case exception names must stay in the language of their layer",
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
                + "use-case-exception role, the vocabulary's own code excluded: the namespaces the configured "
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
            "A namespace that drives the application and knows neither failure type either lets everything"
                + " escape to a generic handler or catches a generic type and answers every outcome the same"
                + " way; whether it does is not visible in the type model",
            arch =>
            {
                var domainException = arch.Layout.Markers.DomainException;
                var useCaseException = arch.Layout.Markers.UseCaseException;
                var anyFailureType = arch.Types.Any(t =>
                    t.FullName != domainException && t.FullName != useCaseException
                    && (IsAssignableTo(t, domainException) || IsAssignableTo(t, useCaseException)));
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

                var observed = new List<string>();
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
                    if (targets.Any(t => IsAssignableTo(t, arch.Layout.Markers.DomainException) || IsAssignableTo(t, arch.Layout.Markers.UseCaseException)))
                    {
                        namespacesThatName.Add(ns);
                    }

                    if (type is not Interface && targets.Any(t => IsAssignableTo(t, arch.Layout.Markers.InputPort)))
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
                        $"{ns}: drives an input port and names no failure type of the inner layers ({string.Join(", ", names)})");
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
                + "UseCaseException, and never fails. A handler in a different namespace of the same module is "
                + "not seen, and a caught type is not visible to the type model, so this does not establish "
                + "that a namespace translates nothing.");

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
                        && !DcaMarkers.IsRole(type, markers.UseCaseException) && type != typeof(Exception); type = type.BaseType)
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
                "The project's own types below DomainException or UseCaseException, with their runtime type in"
                + " the loaded assemblies, and the instance fields they and their project base types declare -"
                + " the backing fields of auto-properties included.")
            .Checking(
                "Informational diagnostic only: lists a field whose type is a plain string, or is assignable to"
                + " IAggregateRoot or IEntity - directly, as an array element or as a generic type argument -"
                + " and never fails. Ids, values, numbers and every other type pass. A string that holds a code"
                + " rather than display text is listed as well: the type does not tell the two apart, and the"
                + " remedy is a typed value for the code.");

    /// <summary>What is wrong with a failure's field type, worded for the line, or <c>null</c>.</summary>
    private static string? FieldFinding(Type type, DcaMarkers markers)
    {
        if (type == typeof(string))
        {
            return "a plain string";
        }

        if (DcaMarkers.IsAssignableToByName(type, markers.AggregateRoot))
        {
            return $"the aggregate {type.Name}";
        }

        if (DcaMarkers.IsAssignableToByName(type, markers.Entity))
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

    /// <summary>The project's own types below one of the two exception base types.</summary>
    private static IEnumerable<IType> ExceptionsOfEitherLayer(DcaArchitecture arch) =>
        ProjectExceptions(arch)
            .Where(t => IsAssignableTo(t, arch.Layout.Markers.DomainException) || IsAssignableTo(t, arch.Layout.Markers.UseCaseException));

    private static bool IsAssignableTo(IType type, string target) => type.IsAssignableTo(target);
}
