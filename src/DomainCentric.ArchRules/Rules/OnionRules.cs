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
            arch => Types().That().ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllDomainPatterns()))
                .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllApplicationPatterns())))
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
                var domain = new Regex(DcaLayout.AnyOf(arch.AllDomainPatterns()));
                // The layout's allow-list may name the whole building-blocks namespace (the default does, so
                // that attribute rules accept every marker); for *dependencies* only the tactical markers and
                // the output ports belong in the domain - strategic annotations and input ports do not.
                var allowedPrefixes = Layout.ThirdPartyNamespacesAllowedInDomain
                    .Where(p => !DcaLayout.IsBelow(p, DcaLayout.BuildingBlocksNamespace))
                    .Concat(new[] { DcaLayout.BuildingBlocksTacticalNamespace, DcaLayout.BuildingBlocksPortsOutNamespace })
                    .ToList();
                bool Allowed(string ns) => domain.IsMatch(ns) || allowedPrefixes.Any(p => DcaLayout.IsBelow(ns, p));

                var violations = arch.Types
                    .Where(t => t.Namespace is not null && domain.IsMatch(t.Namespace.FullName))
                    .SelectMany(t => t.Dependencies
                        .Select(d => d.Target)
                        .Where(target => target.Namespace is not null && !string.IsNullOrEmpty(target.Namespace.FullName))
                        .Where(target => !Allowed(target.Namespace.FullName))
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
                + "allow-list (by default System and Microsoft.Extensions.Logging.Abstractions, "
                + "plus whatever the layout adds) and, of the building blocks, only Ddd.Tactical "
                + "and Hexagonal.Ports.Out - a building-blocks entry in the allow-list is ignored "
                + "here, so the strategic annotations and the input ports are not allowed in the "
                + "domain. A dependency on any other namespace is reported, each distinct pair once.");
    }

    /// <summary>
    /// Domain models (and their members) carry no attributes from outside the layout's allow-list —
    /// the .NET reading of "no Spring/JPA annotations".
    /// </summary>
    public IDcaRule DomainModelsMustNotHaveFrameworkAttributes()
    {
        const string title = "Domain Models must not have framework attributes";
        const string rationale = "Domain models must be framework-independent (no DI-container or ORM attributes)";
        return DcaRule.Check(
            "DCA-ONI-003",
            title,
            rationale,
            arch =>
            {
                var domainModel = new Regex(DcaLayout.AnyOf(arch.AllDomainModelPatterns()));
                bool Allowed(ArchUnitNET.Domain.Attribute a) =>
                    a.Namespace is not null && Layout.ThirdPartyNamespacesAllowedInDomain.Any(p => DcaLayout.IsBelow(a.Namespace.FullName, p));

                var violations = arch.Types
                    .Where(t => t.Namespace is not null && domainModel.IsMatch(t.Namespace.FullName))
                    .SelectMany(t => t.Attributes.Select(a => (Owner: t.FullName, Attribute: a))
                        .Concat(t.Members.SelectMany(m => m.Attributes.Select(a => (Owner: $"{t.FullName}.{m.Name}", Attribute: a)))))
                    .Where(x => !Allowed(x.Attribute))
                    .Select(x => $"{x.Owner} is annotated with {x.Attribute.FullName}")
                    .Distinct()
                    .ToList();
                DcaRule.Fail($"{title}\nbecause {rationale}", violations);
            })
            .Selecting(
                "Types in <module>.Domain.Model of every module root, the shared kernel's "
                + "included when it owns a domain layer.")
            .Checking(
                "Every attribute on the type or on one of its own, non-inherited members has "
                + "a namespace below a prefix of the layout's third-party allow-list - by "
                + "default System, Microsoft.Extensions.Logging.Abstractions and "
                + "DomainCentric.BuildingBlocks. Any other attribute is reported, whatever "
                + "framework it comes from; types elsewhere in the domain layer "
                + "(Domain.Service, Domain.Event) are not selected.");
    }
}
