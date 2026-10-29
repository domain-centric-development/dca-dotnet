// RetrofitKernel fixture — the conventional shared-kernel namespace, declared by nothing.
namespace DomainCentric.ArchRules.Tests.Fixtures.RetrofitKernel.SharedKernel.Domain.Model
{
    public sealed record Money(long Cents);
}

namespace DomainCentric.ArchRules.Tests.Fixtures.RetrofitKernel.Ordering.Domain.Model
{
    using SharedKernel.Domain.Model;

    public sealed record Order(Money Total);
}
