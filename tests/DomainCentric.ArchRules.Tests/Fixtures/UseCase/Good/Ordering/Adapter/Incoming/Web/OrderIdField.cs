using System;
using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good.Ordering.Adapter.Incoming.Web;

/// <summary>DCA-USE-020: the adapter parses the raw id at the edge and answers malformed text itself.</summary>
public static class OrderIdField
{
    public static OrderId? Read(string text) => Guid.TryParse(text, out var value) ? new OrderId(value) : null;
}
