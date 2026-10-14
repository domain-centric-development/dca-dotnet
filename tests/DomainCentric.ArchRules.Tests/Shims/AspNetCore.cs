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

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>Stand-in for the GET mapping attribute of the default preset (DCA-HEX-014).</summary>
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class HttpGetAttribute : System.Attribute
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class HttpPostAttribute : System.Attribute
    {
    }
}

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>Stand-in for the general verb attribute (DCA-HEX-014): the HTTP methods are its constructor arguments.</summary>
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
    public sealed class AcceptVerbsAttribute : System.Attribute
    {
        public AcceptVerbsAttribute(string method)
            : this(new[] { method })
        {
        }

        public AcceptVerbsAttribute(params string[] methods)
        {
            HttpMethods = methods;
        }

        public System.Collections.Generic.IEnumerable<string> HttpMethods { get; }
    }

    /// <summary>Stand-in for a route template without an HTTP method.</summary>
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = true)]
    public sealed class RouteAttribute : System.Attribute
    {
        public RouteAttribute(string template)
        {
            Template = template;
        }

        public string Template { get; }
    }
}
