using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Good.Ordering.Adapter.Incoming;

/// <summary>DCA-HEX-016: the published code is spelled per value; the adapter's own enum may use its name.</summary>
public static class OrderStatusCode
{
    public static string Of(OrderStatus status) => status switch
    {
        OrderStatus.Open => "open",
        OrderStatus.Shipped => "shipped",
        _ => "unknown",
    };

    public static string Format(Notice notice) => notice.ToString();
}

/// <summary>A record's generated ToString spells the status, but the adapter did not choose that.</summary>
public sealed record OrderStatusView(OrderStatus Status);

public enum Notice
{
    Saved,
}
