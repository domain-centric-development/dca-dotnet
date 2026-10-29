using System;
using System.Collections.Generic;
using System.Linq;

namespace DomainCentric.ArchRules;

/// <summary>
/// How a role or a layer is recognised in the code: by a supertype, an attribute, a namespace or a
/// name suffix — or by any of several. A layout names each layer and each role through one marker, so
/// a project that already marks its code its own way points the rules there instead of renaming its
/// code:
/// <code>
/// DcaLayout.ForRootNamespace("Acme.Billing")
///     .WithOutgoingAdapter(DcaMarker.AnyOf(
///         DcaMarker.OfNamespace("Adapter.Outgoing"),
///         DcaMarker.OfAttribute("Acme.Arch.SecondaryAdapterLayerAttribute")));
/// </code>
/// </summary>
/// <remarks>
/// <para>Not every kind fits every role. A namespace says where a type sits, not what it is: a layer is
/// recognised by its namespace or by an attribute on the type or its assembly; a building block by a
/// supertype or an attribute on the type, never by its namespace. The <c>With…</c> method of a role
/// refuses a kind it does not take.</para>
/// <para>The library ships no attributes of its own for layers or roles: <see cref="OfAttribute"/> exists
/// so that a project can name the attributes it already has. Every name is a full type name, matched
/// against the loaded types by name, so the attribute never has to be referenced by the rules.</para>
/// </remarks>
public sealed class DcaMarker : IEquatable<DcaMarker>
{
    /// <summary>The ways a marker recognises code.</summary>
    public enum Kind
    {
        /// <summary>Assignable to a type — an interface or a base class.</summary>
        Type,

        /// <summary>Carries an attribute — on the type; for a layer also on its assembly, or an assembly attribute naming its namespace.</summary>
        Attribute,

        /// <summary>Lies in a namespace below a module root, e.g. <c>Adapter.Outgoing</c>.</summary>
        Namespace,

        /// <summary>Has a simple name ending with a suffix — a naming convention, not a marker.</summary>
        Suffix,

        /// <summary>Any of several markers.</summary>
        AnyOf,
    }

    private readonly IReadOnlyList<DcaMarker> _members;

    private DcaMarker(Kind kind, string? value, IReadOnlyList<DcaMarker> members)
    {
        MarkerKind = kind;
        Value = value;
        _members = members;
    }

    /// <summary>How this marker recognises code.</summary>
    public Kind MarkerKind { get; }

    /// <summary>The name this marker matches; <c>null</c> for <see cref="Kind.AnyOf"/>.</summary>
    public string? Value { get; }

    /// <summary>Recognises a type assignable to the given interface or class (full type name).</summary>
    public static DcaMarker OfType(string typeName) => new(Kind.Type, RequireName(typeName, nameof(OfType)), Array.Empty<DcaMarker>());

    /// <summary>
    /// Recognises code carrying the given attribute (full type name) — on the type itself or, for a
    /// layer, on the type's assembly, or as an assembly attribute whose first constructor argument names
    /// the type's namespace or a namespace above it.
    /// </summary>
    public static DcaMarker OfAttribute(string attributeName) =>
        new(Kind.Attribute, RequireName(attributeName, nameof(OfAttribute)), Array.Empty<DcaMarker>());

    /// <summary>
    /// Recognises code in the given namespace below a module root — one segment or several, dotted:
    /// <c>"Domain"</c>, <c>"Adapter.Outgoing"</c>.
    /// </summary>
    public static DcaMarker OfNamespace(string namespaceBelowModuleRoot) =>
        new(Kind.Namespace, RequireName(namespaceBelowModuleRoot, nameof(OfNamespace)), Array.Empty<DcaMarker>());

    /// <summary>Recognises a type whose simple name ends with the given suffix.</summary>
    public static DcaMarker OfSuffix(string suffix) => new(Kind.Suffix, RequireName(suffix, nameof(OfSuffix)), Array.Empty<DcaMarker>());

    /// <summary>
    /// Recognises code that any of the given markers recognises — a migration that keeps the old
    /// namespace beside the new attribute, or a project that accepts its own attribute and a library's.
    /// Nested <c>AnyOf</c> markers are flattened; one marker alone is that marker.
    /// </summary>
    public static DcaMarker AnyOf(params DcaMarker[] markers)
    {
        ArgumentNullException.ThrowIfNull(markers);
        var flat = new List<DcaMarker>();
        foreach (var marker in markers)
        {
            if (marker is null)
            {
                throw new ArgumentNullException(nameof(markers), "AnyOf member");
            }

            flat.AddRange(marker.Alternatives);
        }

        if (flat.Count == 0)
        {
            throw new ArgumentException("AnyOf needs at least one marker", nameof(markers));
        }

        return flat.Count == 1 ? flat[0] : new DcaMarker(Kind.AnyOf, null, flat.ToArray());
    }

    /// <summary>The single markers this one stands for — itself, or the members of an <c>AnyOf</c>.</summary>
    public IReadOnlyList<DcaMarker> Alternatives => MarkerKind == Kind.AnyOf ? _members : new[] { this };

    /// <summary>The values of the alternatives of the given kind, in order.</summary>
    public IReadOnlyList<string> ValuesOf(Kind wanted) =>
        Alternatives.Where(m => m.MarkerKind == wanted).Select(m => m.Value!).ToArray();

    /// <summary>
    /// Refuses this marker unless every alternative is of one of the allowed kinds; the message names the
    /// role and the kinds it takes.
    /// </summary>
    internal DcaMarker RequireKinds(string role, params Kind[] allowed)
    {
        foreach (var alternative in Alternatives)
        {
            if (!allowed.Contains(alternative.MarkerKind))
            {
                throw new ArgumentException(
                    role + " is recognised by "
                    + string.Join(" or ", allowed.Select(k => "Of" + Label(k)))
                    + ", not by " + alternative
                    + (alternative.MarkerKind == Kind.Namespace ? " - a namespace says where a type sits, not what it is" : ""));
            }
        }

        return this;
    }

    private static string Label(Kind kind) => kind switch
    {
        Kind.Type => "Type",
        Kind.Attribute => "Attribute",
        Kind.Namespace => "Namespace",
        Kind.Suffix => "Suffix",
        _ => "AnyOf",
    };

    private static string RequireName(string value, string factory)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(factory + " needs a name, was blank", nameof(value));
        }

        return value.Trim();
    }

    public bool Equals(DcaMarker? other) =>
        other is not null
        && MarkerKind == other.MarkerKind
        && Value == other.Value
        && _members.SequenceEqual(other._members);

    public override bool Equals(object? obj) => obj is DcaMarker other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(MarkerKind);
        hash.Add(Value);
        foreach (var member in _members)
        {
            hash.Add(member);
        }

        return hash.ToHashCode();
    }

    /// <summary><c>OfNamespace(Adapter.Outgoing)</c>, <c>AnyOf(OfNamespace(..), OfAttribute(..))</c>.</summary>
    public override string ToString() =>
        MarkerKind == Kind.AnyOf
            ? "AnyOf(" + string.Join(", ", _members.Select(m => m.ToString())) + ")"
            : "Of" + Label(MarkerKind) + "(" + Value + ")";
}
