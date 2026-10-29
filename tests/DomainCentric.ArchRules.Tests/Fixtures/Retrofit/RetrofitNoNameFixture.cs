// RetrofitNoName fixture — an own context attribute that carries no name.
using System;

namespace DomainCentric.ArchRules.Tests.Fixtures.RetrofitNoName
{
    /// <summary>Declares a context but carries no Name.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ContextLabelAttribute : Attribute
    {
        public ContextLabelAttribute(string value) => Value = value;

        public string Value { get; }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.RetrofitNoName.Ordering
{
    [ContextLabel("orders")]
    public static class OrderingContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.RetrofitNoName.Ordering.Domain.Model
{
    public sealed record Order(long Id);
}
