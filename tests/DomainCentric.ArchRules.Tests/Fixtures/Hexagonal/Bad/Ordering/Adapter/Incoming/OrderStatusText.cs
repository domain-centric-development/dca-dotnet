using System;
using DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.Hexagonal.Bad.Ordering.Adapter.Incoming;

/// <summary>DCA-HEX-016: every way the IL shows a domain enum turned into its member name.</summary>
public static class OrderStatusText
{
    public static string Plain(OrderStatus status) => status.ToString();

    public static string Formatted(OrderStatus status) => status.ToString("G");

    public static string Interpolated(OrderStatus status) => $"status={status}";

    public static string Concatenated(OrderStatus status) => "status=" + status;

    public static string Composite(OrderStatus status) => string.Format("status={0}", status);

    public static string? Named(OrderStatus status) => Enum.GetName(status);
}
