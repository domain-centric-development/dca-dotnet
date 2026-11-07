using DomainCentric.BuildingBlocks.Application;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Bad.Booking.Adapter.Outgoing;

/// <summary>DCA-ERR-002: invented by the adapter instead of declared beside the port.</summary>
public sealed class StoreRefusedReservationException : OutputPortException
{
    public StoreRefusedReservationException(string id)
        : base($"Store refused reservation {id}")
    {
    }
}
