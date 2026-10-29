// Bare-values fixture — DCA-TAC-025 passes over a failure and over a value's own error shapes, and tells a one-field
// type without the value role to carry it.
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.BareValues.App.Orders
{
    [BoundedContext("Orders")]
    public static class OrdersContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.BareValues.App.Orders.Domain.Model
{
    /// <summary>A failure reports what was refused, the raw reason included.</summary>
    public sealed class OrderRefused : DomainException
    {
        public OrderRefused(string reason)
            : base("Order refused: " + reason) => Reason = reason;

        public string Reason { get; }
    }

    /// <summary>A wrapper value whose own error shapes carry the input it refused.</summary>
    public sealed record PostalCode(string Value) : IValue
    {
        /// <summary>Why a text is no postal code.</summary>
        public interface IError
        {
        }

        /// <summary>The refused input was empty.</summary>
        public sealed record EmptyError(string FieldValue) : IError;

        /// <summary>The refused input was too long.</summary>
        public sealed record TooLongError(string FieldValue, int Length) : IError;
    }

    /// <summary>One field, but no value marker - reported, with the hint to mark it.</summary>
    public sealed record Street(string Name);
}
