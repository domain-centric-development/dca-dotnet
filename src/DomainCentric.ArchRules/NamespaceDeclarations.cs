using System;
using System.Reflection;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DomainCentric.ArchRules;

/// <summary>
/// Answers a project's own context or shared-kernel attribute as the library attribute. Its members are read
/// by name — the public property of that name, which is where an attribute keeps a constructor argument too —
/// and never through a type literal of the project's attribute. A member the library attribute requires and the
/// project's attribute lacks is reported with the namespace and the member it was asked for; an optional
/// member it lacks keeps the library default.
/// </summary>
internal static class NamespaceDeclarations
{
    internal static Attribute As(Type libraryType, object attribute, string ns)
    {
        if (libraryType == typeof(BoundedContextAttribute))
        {
            return new BoundedContextAttribute(Member(attribute, nameof(BoundedContextAttribute.Name), libraryType, ns, required: true)!)
            {
                Description = Member(attribute, nameof(BoundedContextAttribute.Description), libraryType, ns, required: false) ?? "",
            };
        }

        if (libraryType == typeof(SharedKernelAttribute))
        {
            return new SharedKernelAttribute
            {
                Description = Member(attribute, nameof(SharedKernelAttribute.Description), libraryType, ns, required: false) ?? "",
            };
        }

        throw new ArgumentException($"{libraryType.FullName} is not a namespace declaration a marker role names", nameof(libraryType));
    }

    private static string? Member(object attribute, string member, Type libraryType, string ns, bool required)
    {
        var property = attribute.GetType().GetProperty(member, BindingFlags.Public | BindingFlags.Instance);
        if (property is not null && property.GetIndexParameters().Length == 0)
        {
            return property.GetValue(attribute)?.ToString() ?? "";
        }

        if (!required)
        {
            return null;
        }

        throw new InvalidOperationException(
            $"[{attribute.GetType().FullName}] on namespace {ns} stands in for [{libraryType.FullName}], whose member"
            + $" {member} the rules read - the attribute declares no public property of that name. Add it, or point"
            + " the marker role back at the library's attribute.");
    }
}
