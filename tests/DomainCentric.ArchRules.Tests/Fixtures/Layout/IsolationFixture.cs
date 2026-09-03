// Isolation fixture: the identical violation in two places — Peer, a declared context, and Reporting,
// an undeclared module — so the difference is the declaration and nothing else. Isolation is structural,
// so both are reported, and the undeclared module is protected as a target too.
using DomainCentric.BuildingBlocks.Ddd.Strategic;

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Isolation.Catalog
{
    [BoundedContext("Catalog", Description = "Declared context with an internal domain and a published api")]
    public static class CatalogContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Isolation.Catalog.Api
{
    /// <summary>The published contract — the Api namespace is where public types live.</summary>
    public interface ICatalogService
    {
        string Describe(string sku);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Isolation.Catalog.Domain.Model
{
    /// <summary>Internal. Nobody outside the catalog context may touch this.</summary>
    public sealed record Product(string Sku);
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Isolation.Catalog.Application.Describe
{
    using Reporting.Application.GetReport;

    /// <summary>
    /// The target side: a declared context reaching into an <em>undeclared</em> module's application
    /// layer. Isolation must protect the undeclared module as a target, not only govern it as a source.
    /// </summary>
    public sealed class DescribeProductUseCase
    {
        public string Describe(GetReportUseCase report) => report.ToString() ?? string.Empty;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Isolation.Peer
{
    [BoundedContext("Peer", Description = "Declared context that reaches into the catalog's internals")]
    public static class PeerContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Isolation.Peer.Application.GetPeer
{
    using Catalog.Domain.Model;

    /// <summary>Control group: a declared context importing another module's internal domain type.</summary>
    public sealed class GetPeerUseCase
    {
        public string Describe(Product product) => product.Sku;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Isolation.Peer.Adapter.Outgoing.Catalog
{
    using Isolation.Catalog.Api;
    using Isolation.Catalog.Domain.Model;

    /// <summary>Allowed: an outgoing adapter depending on another module's published Api namespace.</summary>
    public sealed class CatalogClient
    {
        private readonly ICatalogService _catalog;

        public CatalogClient(ICatalogService catalog)
        {
            _catalog = catalog;
        }

        public string Describe(string sku) => _catalog.Describe(sku);
    }

    /// <summary>Forbidden: an outgoing adapter depending on another module's domain model (DCA-STR-006).</summary>
    public sealed class CatalogInternalsClient
    {
        public string Describe(Product product) => product.Sku;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Isolation.Peer.Domain.Model
{
    using Catalog.Api;

    /// <summary>Forbidden: a domain layer may not depend on another module at all, not even its Api (DCA-STR-004).</summary>
    public sealed record PeerListing(ICatalogService Catalog);
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Isolation.Reporting.Application.GetReport
{
    using Catalog.Domain.Model;

    /// <summary>
    /// The measurement. Same violation as the control group, but this module declares no
    /// <c>[BoundedContext]</c> — and is a subject of the isolation rules all the same.
    /// </summary>
    public sealed class GetReportUseCase
    {
        public string Describe(Product product) => product.Sku;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.Isolation.Reporting.Adapter.Incoming.Web
{
    using Catalog.Api;

    /// <summary>
    /// Forbidden: an incoming adapter of an <em>undeclared</em> module orchestrating another module
    /// (DCA-HEX-007). Structural selection makes the undeclared module a subject here too.
    /// </summary>
    public sealed class ReportController
    {
        public string Report(ICatalogService catalog) => catalog.Describe("sku");
    }
}
