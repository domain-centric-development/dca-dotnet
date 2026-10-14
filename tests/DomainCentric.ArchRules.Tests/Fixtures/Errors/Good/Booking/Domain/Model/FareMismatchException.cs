using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Domain.Model;

/// <summary>DCA-ERR-007: carries the two codes the rule compared, each as a typed value, and the kind as an enum.</summary>
public sealed class FareMismatchException : DomainException
{
    public FareMismatchException(FareCode left, FareCode right, SeatKind kind)
        : base($"cannot combine fares {left.Value} and {right.Value} for {kind}")
    {
        Left = left;
        Right = right;
        Kind = kind;
    }

    public FareCode Left { get; }

    public FareCode Right { get; }

    public SeatKind Kind { get; }
}
