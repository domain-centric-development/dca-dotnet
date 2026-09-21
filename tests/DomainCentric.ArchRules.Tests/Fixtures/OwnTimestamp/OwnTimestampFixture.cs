using System;

namespace DomainCentric.ArchRules.Tests.Fixtures.OwnTimestamp.Vocabulary
{
    /// <summary>A code base's own domain-event marker, unrelated to the library's.</summary>
    public interface IHappening
    {
        Timestamp OccurredOn { get; }
    }

    /// <summary>The code base wraps its occurrence time in a value type, as DCA teaches for primitives.</summary>
    public readonly record struct Timestamp(DateTimeOffset Value)
    {
        public static Timestamp Now() => new(DateTimeOffset.UtcNow);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.OwnTimestamp.Billing.Domain.Model
{
    using DomainCentric.ArchRules.Tests.Fixtures.OwnTimestamp.Vocabulary;

    /// <summary>Stores its occurrence time — in the code base's own type, not in one of System.</summary>
    public sealed record TariffBilled(Guid EventId, Timestamp OccurredOn) : IHappening;
}
