using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Good.Booking.Domain.Model;

/// <summary>
/// Status is Ubiquitous Language here, not a transport concept: DCA-ERR-005 matches the compound
/// transport words, not the bare word, so this name passes.
/// </summary>
public sealed class ReservationStatusInvalidException : DomainException
{
    public ReservationStatusInvalidException(ReservationId id)
        : base($"reservation {id.Value} is in a status that does not allow this")
    {
    }
}
