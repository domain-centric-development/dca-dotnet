using System;

namespace DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Domain.Util;

// DCA-NAM-009: technical bucket namespace
public static class Dates
{
    public static DateTimeOffset Now() => DateTimeOffset.UtcNow;
}
