using DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Alpha.Application.Booking;

namespace DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Alpha.Application.Quote;

// DCA-CYC-005: Quote <-> Booking
public sealed class QuoteUseCase
{
    private readonly BookingUseCase? _booking;
}
