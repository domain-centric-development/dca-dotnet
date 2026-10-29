using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace DomainCentric.ArchRules.Rules;

/// <summary>
/// Onion Architecture rules: the domain is the innermost layer — it depends on nothing but itself,
/// the building-blocks markers and a short allow-list of third-party namespaces.
/// </summary>
public sealed class OnionRules : IDcaRuleSet
{
    /// <summary>Java rules of this set that have no .NET reading (none).</summary>
    public static readonly IReadOnlyDictionary<string, string> NotApplicable = new Dictionary<string, string>();

    public OnionRules(DcaLayout layout)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Rules = new List<IDcaRule>
        {
            DomainMustNotAccessApplication(),
            DomainMustBeFrameworkIndependent(),
            DomainModelsMustNotHaveFrameworkAttributes(),
        }.AsReadOnly();
    }

    public string Name => "onion";

    public IReadOnlyList<IDcaRule> Rules { get; }

    /// <summary>The layout this rule set was built for.</summary>
    public DcaLayout Layout { get; }

    public IDcaRule DomainMustNotAccessApplication() =>
        DcaRule.Of(
            "DCA-ONI-001",
            "Domain must not access Application Services (Onion Architecture - Domain is innermost layer)",
            "Domain is the innermost layer in onion architecture and should not depend on application services",
            arch => Types().That().FollowCustomPredicate(arch.InDomain, "are in the domain layer")
                .Should().NotDependOnAnyTypesThat().FollowCustomPredicate(arch.InApplication, "are in the application layer"))
            .Selecting(
                "Types whose namespace lies under <Module>.Domain of every module root - "
                + "declared bounded contexts, the shared kernel when it owns a domain layer, "
                + "and undeclared modules alike, at any depth below the root namespace.")
            .Checking(
                "No dependency on a type whose namespace lies under <Module>.Application of "
                + "any module root, the module's own included. Dependencies on adapters or "
                + "infrastructure are covered by other rules, not this one; an empty selection "
                + "passes.");

    /// <summary>
    /// Domain types may depend only on domain namespaces (matched by pattern, which also covers the
    /// shared kernel's domain), the building blocks and the layout's third-party allow-list.
    /// </summary>
    public IDcaRule DomainMustBeFrameworkIndependent()
    {
        const string title =
            "The Domain Model should be framework independent and should not use 3rd party libraries when possible";
        const string rationale = "Domain should be framework-independent (Dependency Inversion Principle)";
        return DcaRule.Check(
            "DCA-ONI-002",
            title,
            rationale,
            arch =>
            {
                // The layout's allow-list may name the whole building-blocks namespace (the default does, so
                // that attribute rules accept every marker); for *dependencies* only the namespaces the
                // configured vocabulary declares its domain-facing roles in belong in the domain - the
                // application-layer roles, the strategic attributes and the input ports do not. Derived from
                // the roles rather than hard-wired, so a project that points the roles at its own markers is
                // not reported as depending on a foreign library inside its own domain.
                var allowedPrefixes = Layout.ThirdPartyNamespacesAllowedInDomain
                    .Where(p => !DcaLayout.IsBelow(p, DcaLayout.BuildingBlocksNamespace))
                    .Concat(Layout.Markers.DeclaringNamespacesOf(DcaMarkers.DomainFacingRoles))
                    .ToList();
                bool Allowed(IType target) =>
                    arch.InDomain(target) || allowedPrefixes.Any(p => DcaLayout.IsBelow(target.Namespace.FullName, p));

                var violations = arch.Types
                    .Where(t => t.Namespace is not null && arch.InDomain(t))
                    .SelectMany(t => t.Dependencies
                        .Select(d => d.Target)
                        .Where(target => target.Namespace is not null && !string.IsNullOrEmpty(target.Namespace.FullName))
                        .Where(target => !Allowed(target))
                        .Select(target => $"{t.FullName} depends on {target.FullName}"))
                    .Distinct()
                    .ToList();
                DcaRule.Fail($"{title}\nbecause {rationale}", violations);
            })
            .Selecting(
                "Types below the root namespace whose namespace lies under <Module>.Domain of "
                + "every module root; the building-blocks types themselves are not selected.")
            .Checking(
                "Every dependency whose target has a namespace points into one of those same "
                + "domain namespaces or below an allowed prefix: the layout's third-party "
                + "allow-list (by default System only - a logging, validation or utility library "
                + "is a project decision, added with WithThirdPartyNamespacesAllowedInDomain) and "
                + "the namespaces the configured marker "
                + "vocabulary declares its domain-facing roles in - the tactical roles and the "
                + "outgoing ports, which for the default vocabulary are Ddd.Tactical and "
                + "Hexagonal.Ports.Out. The vocabulary's own namespaces are on the list because "
                + "they are derived from the roles, so pointing a role at another library's type "
                + "does not make that library a reported dependency. A building-blocks entry in "
                + "the allow-list is ignored here, so the strategic attributes, the "
                + "application-layer roles and the input ports are not allowed in the domain. The "
                + "shared kernel is not on the list either, unless it is a module root with a "
                + "Domain layer of its own. A dependency on any other namespace is reported, each "
                + "distinct pair once.");
    }

    /// <summary>Domain-model metadata is classified by configured prohibited roles.</summary>
    public IDcaRule DomainModelsMustNotHaveFrameworkAttributes() =>
        DcaRule.Check("DCA-ONI-003", "Domain models must not carry prohibited framework metadata",
            "Domain metadata does not configure infrastructure concerns",
            arch => DomainMetadata.Check(arch, "DCA-ONI-003"))
        .Selecting("Domain.Model types except events, services, factories and specifications, which have exclusive ADV ownership.")
        .Checking("Configured attribute namespaces (and the explicit persistence attribute type names) classify an attribute or any base attribute type. Types prohibit container, persistence and transaction roles; fields and properties prohibit injection and persistence; methods prohibit transaction and injection; constructors prohibit injection. Unclassified metadata is allowed by this check, not proven harmless: the default preset classifies its configured persistence namespaces, the key/timestamp/concurrency attribute types and keyed-service injection; mappings of other persistence libraries need a preset extension. The container role is empty in both shipped presets: .NET registers services in code, not through an attribute, so the half of this rule that does the most work in Java - a container stereotype on a domain type - selects nothing here until a project names its own. Missing runtime types are skipped; wiring is not established.");
}
