using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Domain.Model;

/// <summary>
/// DCA-ERR-007: carries two codes as plain strings. Nothing in the type tells a code from display text, so both
/// are listed - the remedy is a typed value for the code.
/// </summary>
public sealed class FareMismatchException : DomainException
{
    public FareMismatchException(string left, string right)
        : base($"cannot combine fares {left} and {right}")
    {
        Left = left;
        Right = right;
    }

    public string Left { get; }

    public string Right { get; }
}
