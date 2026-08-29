using System;

namespace DomainCentric.ArchRules;

/// <summary>
/// Full names of framework types and attributes the DCA rules refer to. The rule library has no
/// compile-time dependency on any web or DI framework; types are matched by name so the same rules
/// work with ASP.NET Core MVC, Razor Pages, Minimal APIs or another host.
/// </summary>
/// <remarks>
/// .NET has no stereotype attributes comparable to Spring's <c>@Service</c>/<c>@Component</c> — services
/// are registered in a container by code. Rules of the Java catalog that depend on such stereotypes
/// have no .NET counterpart and are marked as such in the rule catalog.
/// </remarks>
/// <param name="ControllerBase">Base class of MVC / API controllers.</param>
/// <param name="ApiControllerAttribute">Attribute marking API controllers.</param>
/// <param name="PageModelBase">Base class of Razor Pages page models.</param>
/// <param name="TransactionScope">Type used for explicit transaction demarcation.</param>
public sealed record FrameworkTypes(
    string ControllerBase,
    string ApiControllerAttribute,
    string PageModelBase,
    string TransactionScope)
{
    /// <summary>ASP.NET Core MVC / Razor Pages and <c>System.Transactions</c>.</summary>
    public static FrameworkTypes AspNetCore() =>
        new(
            "Microsoft.AspNetCore.Mvc.ControllerBase",
            "Microsoft.AspNetCore.Mvc.ApiControllerAttribute",
            "Microsoft.AspNetCore.Mvc.RazorPages.PageModel",
            "System.Transactions.TransactionScope");
}
