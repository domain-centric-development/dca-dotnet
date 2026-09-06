using DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Alpha.Application.Quote;

namespace DomainCentric.ArchRules.Tests.Fixtures.Cycles.Bad.Alpha.Application.Booking;

public sealed class BookingUseCase
{
    private readonly QuoteUseCase? _quote;
}
