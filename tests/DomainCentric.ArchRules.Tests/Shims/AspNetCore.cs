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

    /// <summary>Stand-in for the attribute that keeps a property in TempData (DCA-HEX-015).</summary>
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public sealed class TempDataAttribute : System.Attribute
    {
    }
}

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>Stand-in for the message store held for the next request (DCA-HEX-015).</summary>
    public interface ITempDataDictionary : System.Collections.Generic.IDictionary<string, object?>
    {
    }
}

namespace Microsoft.AspNetCore.Http
{
    /// <summary>Stand-in for the server session (DCA-HEX-015).</summary>
    public interface ISession
    {
        void SetString(string key, string value);
    }
}
