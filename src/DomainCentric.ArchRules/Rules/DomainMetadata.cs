using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Rules;

/// <summary>Exclusive ownership and configurable role-by-target domain metadata policy.</summary>
internal static class DomainMetadata
{
    internal static string Owner(IType type)
    {
        if (type.IsAssignableTo(typeof(IDomainEvent).FullName!)) return "DCA-ADV-004";
        if (type.IsAssignableTo(typeof(IDomainService).FullName!)) return "DCA-ADV-011";
        if (type.IsAssignableTo(typeof(IFactory).FullName!)) return "DCA-ADV-015";
        return type.Name.EndsWith("Specification", StringComparison.Ordinal) ? "DCA-ADV-018" : "DCA-ONI-003";
    }

    internal static void Check(DcaArchitecture arch, string id)
    {
        var roles = arch.Layout.FrameworkTypes;
        var violations = new List<string>();
        foreach (var type in arch.Types.Where(t => t is not Interface && Owner(t) == id
            && Regex.IsMatch(t.Namespace?.FullName ?? "", DcaLayout.AnyOf(arch.AllDomainPatterns()))))
        {
            if (id == "DCA-ONI-003" && !Regex.IsMatch(type.Namespace?.FullName ?? "", DcaLayout.AnyOf(arch.AllDomainModelPatterns()))) continue;
            var runtime = arch.RuntimeType(type);
            if (runtime is null) continue;
            Inspect(runtime, violations, roles.PersistenceAttributeTypes, roles.ContainerAttributeNamespaces, roles.PersistenceAttributeNamespaces, roles.TransactionAttributeNamespaces);
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (var field in runtime.GetFields(flags)) Inspect(field, violations, roles.PersistenceAttributeTypes, roles.InjectionAttributeNamespaces, roles.PersistenceAttributeNamespaces);
            foreach (var property in runtime.GetProperties(flags)) Inspect(property, violations, roles.PersistenceAttributeTypes, roles.InjectionAttributeNamespaces, roles.PersistenceAttributeNamespaces);
            foreach (var method in runtime.GetMethods(flags)) Inspect(method, violations, Array.Empty<string>(), roles.TransactionAttributeNamespaces,
                id == "DCA-ADV-004" ? Array.Empty<string>() : roles.InjectionAttributeNamespaces);
            foreach (var ctor in runtime.GetConstructors(flags)) Inspect(ctor, violations, Array.Empty<string>(), roles.InjectionAttributeNamespaces);
        }
        DcaRule.Fail(id + ": prohibited domain metadata", violations);
    }

    private static void Inspect(MemberInfo target, List<string> violations, IReadOnlyList<string> typeNames, params IReadOnlyList<string>[] roles)
    {
        foreach (var attribute in target.GetCustomAttributesData())
        {
            for (var type = attribute.AttributeType; type is not null; type = type.BaseType)
            {
                if (!typeNames.Contains(type.FullName ?? "") && !roles.SelectMany(r => r).Any(prefix => DcaLayout.IsBelow(type.Namespace ?? "", prefix))) continue;
                violations.Add($"{target.DeclaringType?.FullName ?? (target as Type)?.FullName}.{target.Name} carries prohibited metadata {attribute.AttributeType.FullName}");
                break;
            }
        }
    }
}
