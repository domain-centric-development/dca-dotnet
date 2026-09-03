// A bounded context in transaction-script style: a use case over a store, no aggregate, no Domain
// namespace at all. A generic or supporting subdomain may legitimately look like this, and the whole
// catalog must be green for it without configuration.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.TransactionScript.Reporting
{
    [BoundedContext("Reporting", Description = "Read-only operational reporting, transaction-script style")]
    public static class ReportingContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.TransactionScript.Reporting.Application.Shared
{
    public interface ISalesReportStore : IStore
    {
        Task<IReadOnlyList<string>> FindRecentOrderIdsAsync(int limit, CancellationToken cancellationToken = default);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.TransactionScript.Reporting.Application.GetSalesReport
{
    using Shared;

    public sealed record GetSalesReportQuery(int Limit);

    public sealed record GetSalesReportResult(IReadOnlyList<string> OrderIds);

    public interface IGetSalesReportInputPort : IUseCase<GetSalesReportQuery, GetSalesReportResult>
    {
    }

    public sealed class GetSalesReportUseCase : IGetSalesReportInputPort
    {
        private readonly ISalesReportStore _store;

        public GetSalesReportUseCase(ISalesReportStore store)
        {
            _store = store;
        }

        public async Task<GetSalesReportResult> ExecuteAsync(GetSalesReportQuery input, CancellationToken cancellationToken = default) =>
            new(await _store.FindRecentOrderIdsAsync(input.Limit, cancellationToken));
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.TransactionScript.Reporting.Adapter.Incoming.Web
{
    using Application.GetSalesReport;

    public sealed record SalesReportResponse(IReadOnlyList<string> OrderIds);

    public sealed class SalesReportController
    {
        private readonly IGetSalesReportInputPort _getSalesReport;

        public SalesReportController(IGetSalesReportInputPort getSalesReport)
        {
            _getSalesReport = getSalesReport;
        }

        public async Task<SalesReportResponse> RecentAsync(int limit, CancellationToken cancellationToken = default) =>
            new((await _getSalesReport.ExecuteAsync(new GetSalesReportQuery(limit), cancellationToken)).OrderIds);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Layout.TransactionScript.Reporting.Adapter.Outgoing.Persistence
{
    using Application.Shared;

    public sealed class InMemorySalesReportStore : ISalesReportStore
    {
        public Task<IReadOnlyList<string>> FindRecentOrderIdsAsync(int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(new List<string>());
    }
}
