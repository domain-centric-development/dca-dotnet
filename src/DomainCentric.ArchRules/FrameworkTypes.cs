using System;

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
    /// <summary>ASP.NET Core MVC / Razor Pages and <c>System.Transactions</c>.</summary>
    public static FrameworkTypes AspNetCore() =>
        new(
            "aspnetcore",
            "Microsoft.AspNetCore.Mvc.ControllerBase",
            "Microsoft.AspNetCore.Mvc.ApiControllerAttribute",
            "Microsoft.AspNetCore.Mvc.RazorPages.PageModel",
            "System.Transactions.TransactionScope");

    /// <summary>
    /// No framework types at all — a hand-hosted application, or a framework this library has no preset
    /// for yet (build it up with a <c>with</c> expression). Controllers are then recognised by suffix
    /// only, and the transaction rule has nothing to look for.
    /// </summary>
    public static FrameworkTypes None() => new("none", "", "", "", "");

    /// <summary>Whether a role is configured (non-blank).</summary>
    public static bool IsSet(string role) => !string.IsNullOrWhiteSpace(role);

    public override string ToString() => Name;
}
