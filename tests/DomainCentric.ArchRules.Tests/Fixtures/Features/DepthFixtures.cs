// DCA-USE-014 fixtures — one shape per root namespace, module root = <Shape>.Ordering.

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Depth.Flat.Ordering.Application.PlaceOrder
{
    public sealed class PlaceOrderUseCase
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Depth.Flat.Ordering.Application.CancelOrder
{
    public sealed class CancelOrderUseCase
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Depth.Flat.Ordering.Application.Shared
{
    public interface IOrderRepository
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Depth.Grouped.Ordering.Application.Ordering.PlaceOrder
{
    public sealed class PlaceOrderUseCase
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Depth.Grouped.Ordering.Application.Ordering.PlaceOrder
{
    public interface IPlaceOrderInputPort
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Depth.Grouped.Ordering.Application.Fulfilment.ShipOrder
{
    public sealed class ShipOrderUseCase
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Depth.Grouped.Ordering.Application.Fulfilment.ShipOrder
{
    public sealed record ShipOrderResult(string Label)
    {
        /// <summary>A nested helper named like a use case does not define another use case.</summary>
        public sealed class LabelUseCase
        {
        }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Depth.Grouped.Ordering.Application.Shared
{
    public interface IOrderRepository
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Depth.OverDeep.Ordering.Application.Ordering.Placement.PlaceOrder
{
    public sealed class PlaceOrderUseCase
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Depth.Mixed.Ordering.Application.PlaceOrder
{
    public sealed class PlaceOrderUseCase
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Depth.Mixed.Ordering.Application.Fulfilment.ShipOrder
{
    public sealed class ShipOrderUseCase
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Depth.Shallow.Ordering.Application
{
    public sealed class PlaceOrderUseCase
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Depth.Single.Ordering.Application.Ordering.PlaceOrder
{
    public sealed class PlaceOrderUseCase
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Depth.SingleFlat.Ordering.Application.PlaceOrder
{
    public sealed class PlaceOrderUseCase
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Depth.NoUseCase.Ordering.Application.Shared
{
    public interface IOrderRepository
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Depth.Grouped.Ordering.Application.Support
{
    /// <summary>An abstract base class is not a use case and does not define a use case namespace depth.</summary>
    public abstract class BaseUseCase
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Depth.Flat.Ordering.Application.Support
{
    /// <summary>Sits one level deeper than the flat use cases; ignored because it is abstract.</summary>
    public abstract class BaseUseCase
    {
    }
}
