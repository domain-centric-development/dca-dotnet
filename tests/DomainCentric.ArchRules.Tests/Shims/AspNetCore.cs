// Framework shims: the test project has no web/ORM framework dependency. These types carry the exact
// full names the default DcaLayout.FrameworkTypes (and the Onion fixtures) refer to, so the fixtures
// can exercise the rules without pulling in ASP.NET Core or EF Core.
namespace Microsoft.AspNetCore.Mvc
{
    public abstract class ControllerBase
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class ApiControllerAttribute : System.Attribute
    {
    }
}

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public abstract class PageModel
    {
    }
}

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>Stand-in for an ORM mapping attribute — used to show a framework attribute on a domain model.</summary>
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Property)]
    public sealed class KeylessAttribute : System.Attribute
    {
    }
}
