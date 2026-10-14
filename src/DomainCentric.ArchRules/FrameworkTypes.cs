using System;
using System.Collections.Generic;

namespace DomainCentric.ArchRules;

/// <summary>
/// The framework types the DCA rules refer to, by <em>role</em>. The rule library has no compile-time
/// dependency on any web or DI framework; types are matched by full name, so the same rules govern an
/// ASP.NET Core MVC, Razor Pages, Minimal API or hand-hosted code base.
/// </summary>
/// <remarks>
/// <para>.NET frameworks express these roles as base classes and attributes rather than as stereotype
/// annotations, and there is no injectable stereotype at all — services are registered in a container
/// by code. Rules of the Java catalog that depend on such a stereotype have no .NET counterpart and are
/// listed as not applicable in the rule catalog.</para>
/// <para>Start from a preset — <see cref="AspNetCore"/> (the default of <see cref="DcaLayout"/>) or
/// <see cref="None"/> — and adjust single roles with a <c>with</c> expression:
/// <c>FrameworkTypes.AspNetCore() with { TransactionScope = "Acme.Platform.UnitOfWork" }</c>. An empty
/// role means the framework has no such type; rules that need it then select nothing and pass, as their
/// <c>Checks</c> text documents.</para>
/// </remarks>
/// <param name="Name">The preset's name, shown by <see cref="DcaLayout.ToString"/> so a report says which
/// vocabulary the rules resolved.</param>
/// <param name="ControllerBase">Base class of server-rendering and API controllers.</param>
/// <param name="ApiControllerAttribute">Attribute marking API controllers.</param>
/// <param name="PageModelBase">Base class of page models (server-rendered pages without a controller).</param>
/// <param name="TransactionScope">Type used for explicit transaction demarcation.</param>
public sealed record FrameworkTypes(
    string Name,
    string ControllerBase,
    string ApiControllerAttribute,
    string PageModelBase,
    string TransactionScope)
{
    /// <summary>Optional declarative transaction attribute used to cover publication entry paths.</summary>
    public string TransactionalAttribute { get; init; } = "";

    /// <summary>Full type names of the APIs code <em>uses</em> to run a transaction beside <see cref="TransactionScope"/> — committable transactions, connection transactions, a persistence library's transaction handle. Like the scope they belong to the application layer and the outgoing adapters; any other type that depends on one is reported.</summary>
    public IReadOnlyList<string> TransactionApiTypes { get; init; } = Array.Empty<string>();

    /// <summary>Full type names a composition root <em>declares or wires</em> (a transaction manager). The global and the shared kernel's infrastructure may depend on them; the domain, incoming adapters and a module's own infrastructure may not. Empty by default — the platform has no such type; a project's own manager abstraction is added via a with-expression.</summary>
    public IReadOnlyList<string> TransactionManagerTypes { get; init; } = Array.Empty<string>();

    /// <summary>Attribute namespaces classified as persistence metadata, including derived attributes.</summary>
    public IReadOnlyList<string> PersistenceAttributeNamespaces { get; init; } = Array.Empty<string>();
    /// <summary>Full attribute type names classified as persistence metadata (base types included) — for mapping attributes that live in a namespace whose other attributes are harmless, such as the key/concurrency attributes beside the validation attributes of System.ComponentModel.DataAnnotations. Mappings of other persistence libraries are added per project via a with-expression.</summary>
    public IReadOnlyList<string> PersistenceAttributeTypes { get; init; } = Array.Empty<string>();
    /// <summary>Attribute namespaces classified as injection-site metadata.</summary>
    public IReadOnlyList<string> InjectionAttributeNamespaces { get; init; } = Array.Empty<string>();
    /// <summary>Attribute namespaces classified as transaction metadata.</summary>
    public IReadOnlyList<string> TransactionAttributeNamespaces { get; init; } = Array.Empty<string>();
    /// <summary>Attribute namespaces classified as container stereotypes.</summary>
    public IReadOnlyList<string> ContainerAttributeNamespaces { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Attribute namespaces classified as a web-controller stereotype — the counterpart of the Java
    /// library's <c>webController</c> role. Empty in every preset: ASP.NET Core has no attribute that
    /// makes a class a controller, and none that could sit on an exception. The role exists so that
    /// <c>DCA-ERR-004</c> has the same contract in both languages and a project whose framework does
    /// have such an attribute can name it.
    /// </summary>
    public IReadOnlyList<string> WebControllerAttributeNamespaces { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Attribute namespaces classified as a REST-controller stereotype — the counterpart of the Java
    /// library's <c>restController</c> role. <c>[ApiController]</c> is not classified here: it marks a
    /// controller, not a failure, and no exception can carry it. Empty in every preset, for the same
    /// reason as <see cref="WebControllerAttributeNamespaces"/>.
    /// </summary>
    public IReadOnlyList<string> RestControllerAttributeNamespaces { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Full type names of a module system's per-package module declaration — the counterpart of the
    /// Java library's <c>moduleDeclaration</c> role, where Spring Modulith's <c>@ApplicationModule</c>
    /// fills it. <c>DCA-MAP-006</c> reads the declaration's <c>AllowedDependencies</c> and compares it
    /// with the <c>[Upstream]</c> declarations. Empty in every preset: .NET draws module boundaries
    /// with projects rather than with an attribute, so a project that has such an attribute names it.
    /// </summary>
    public IReadOnlyList<string> ModuleDeclarationAttributeTypes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Attribute namespaces classified as an event-listener stereotype — the counterpart of the Java
    /// library's <c>eventListener</c> role. Empty in every preset: .NET subscribes in code, not through
    /// an attribute.
    /// </summary>
    public IReadOnlyList<string> EventListenerAttributeNamespaces { get; init; } = Array.Empty<string>();

    /// <summary>Full attribute type names that fix the protocol answer of the type they sit on — the role <c>DCA-ERR-004</c> forbids on the exceptions of the domain and application layers, because which status a failure earns is the incoming adapter's decision and a second adapter on another protocol has no use for it. Empty in every preset: the platform answers through a mapper type rather than through an attribute on the failure. A project that defines such an attribute itself adds it via a with-expression, and the Java library's <c>transportStatus</c> role is the same setting under the same rule.</summary>
    public IReadOnlyList<string> TransportStatusAttributeTypes { get; init; } = Array.Empty<string>();

    /// <summary>Full attribute type names that map a handler method to an HTTP GET request (derived attributes included) — the counterpart of the Java library's <c>getMapping</c> role. <c>DCA-HEX-014</c> reads it: a GET handler drives no command. Empty in <see cref="None"/>.</summary>
    public IReadOnlyList<string> GetMappingAttributeTypes { get; init; } = Array.Empty<string>();

    /// <summary>Method-name prefixes by which a framework maps a handler of a controller or page model to an HTTP GET request without an attribute. <c>DCA-HEX-014</c> treats such a method like one carrying a <see cref="GetMappingAttributeTypes"/> attribute. Empty in <see cref="None"/>.</summary>
    public IReadOnlyList<string> GetHandlerNamePrefixes { get; init; } = Array.Empty<string>();

    /// <summary>Always empty, and an assignment is ignored. The role was read only by <c>DCA-HEX-015</c>, which is retired; the property stays so a with-expression written against 0.8.0 still compiles.</summary>
    [Obsolete("DCA-HEX-015 is retired and nothing reads this role; remove the assignment.")]
    public IReadOnlyList<string> InstanceLocalStateTypes { get => Array.Empty<string>(); init { } }

    /// <summary>ASP.NET Core MVC / Razor Pages and <c>System.Transactions</c>.</summary>
    public static FrameworkTypes AspNetCore() =>
        new(
            "aspnetcore",
            "Microsoft.AspNetCore.Mvc.ControllerBase",
            "Microsoft.AspNetCore.Mvc.ApiControllerAttribute",
            "Microsoft.AspNetCore.Mvc.RazorPages.PageModel",
            "System.Transactions.TransactionScope")
        {
            PersistenceAttributeNamespaces = Array.AsReadOnly(new[] { "System.ComponentModel.DataAnnotations.Schema", "Microsoft.EntityFrameworkCore" }),
            PersistenceAttributeTypes = Array.AsReadOnly(new[] { "System.ComponentModel.DataAnnotations.KeyAttribute", "System.ComponentModel.DataAnnotations.TimestampAttribute", "System.ComponentModel.DataAnnotations.ConcurrencyCheckAttribute" }),
            InjectionAttributeNamespaces = Array.AsReadOnly(new[] { "Microsoft.Extensions.DependencyInjection" }),
            TransactionApiTypes = Array.AsReadOnly(new[] { "System.Transactions.CommittableTransaction", "System.Data.IDbTransaction", "System.Data.Common.DbTransaction", "Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction" }),
            GetMappingAttributeTypes = Array.AsReadOnly(new[] { "Microsoft.AspNetCore.Mvc.HttpGetAttribute" }),
            GetHandlerNamePrefixes = Array.AsReadOnly(new[] { "OnGet" }),
        };

    /// <summary>
    /// No framework types at all — a hand-hosted application, or a framework this library has no preset
    /// for yet (build it up with a <c>with</c> expression). Controllers are then recognised by suffix
    /// only, and the transaction rule has nothing to look for.
    /// </summary>
    public static FrameworkTypes None() => new("none", "", "", "", "");

    /// <summary>The names the presets are known under, for <c>dca.framework</c> and error messages.</summary>
    public static IReadOnlyList<string> PresetNames { get; } = Array.AsReadOnly(new[] { "aspnetcore", "none" });

    /// <summary>
    /// The preset registered under the given name, or <c>null</c> when there is none. This is what
    /// <c>dca.framework=&lt;name&gt;</c> in <c>dca-archunit.properties</c> resolves, matching
    /// <c>dca-archunit</c>'s <c>FrameworkAnnotations.preset(name)</c>. Names are compared
    /// case-insensitively.
    /// </summary>
    /// <remarks>
    /// Unlike the Java library this has no provider SPI yet: a framework without a preset here is
    /// configured in code with a <c>with</c> expression on <see cref="None"/>.
    /// </remarks>
    public static FrameworkTypes? Preset(string name) => (name ?? "").Trim().ToLowerInvariant() switch
    {
        "aspnetcore" => AspNetCore(),
        "none" => None(),
        _ => null,
    };

    /// <summary>Whether a role is configured (non-blank).</summary>
    public static bool IsSet(string role) => !string.IsNullOrWhiteSpace(role);

    public override string ToString() => Name;
}
