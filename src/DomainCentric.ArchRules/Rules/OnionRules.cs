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
                .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(DcaLayout.AnyOf(arch.AllApplicationPatterns())));

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
                var allowedPrefixes = Layout.ThirdPartyNamespacesAllowedInDomain
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
            });
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
                var domainModel = new Regex(DcaLayout.AnyOf(arch.AllDomainModelPatterns().Append(Layout.SharedKernelDomainPattern)));
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
            });
    }
}
