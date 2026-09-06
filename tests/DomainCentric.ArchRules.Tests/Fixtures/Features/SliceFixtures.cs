// DCA-CYC-005 fixtures — one shape per root namespace, module root = <Shape>.Ordering.

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.Independent.Ordering.Application.Ordering.PlaceOrder
{
    public sealed class PlaceOrderUseCase
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.Independent.Ordering.Application.Fulfilment.ShipOrder
{
    public sealed class ShipOrderUseCase
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.OneWay.Ordering.Application.Ordering.PlaceOrder
{
    public sealed record PlaceOrderResult(string OrderId);
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.OneWay.Ordering.Application.Ordering.PlaceOrder
{
    public sealed class PlaceOrderUseCase
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.OneWay.Ordering.Application.Fulfilment.ShipOrder
{
    using DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.OneWay.Ordering.Application.Ordering.PlaceOrder;

    public sealed class ShipOrderUseCase
    {
        private readonly PlaceOrderResult? _placeOrderResult;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.Cycle.Ordering.Application.Ordering.PlaceOrder
{
    public sealed record PlaceOrderResult(string OrderId);
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.Cycle.Ordering.Application.Ordering.PlaceOrder
{
    using DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.Cycle.Ordering.Application.Fulfilment.ShipOrder;

    public sealed class PlaceOrderUseCase
    {
        private readonly ShipOrderResult? _shipOrderResult;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.Cycle.Ordering.Application.Fulfilment.ShipOrder
{
    public sealed record ShipOrderResult(string Label);
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.Cycle.Ordering.Application.Fulfilment.ShipOrder
{
    using DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.Cycle.Ordering.Application.Ordering.PlaceOrder;

    public sealed class ShipOrderUseCase
    {
        private readonly PlaceOrderResult? _placeOrderResult;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.Shared.Ordering.Application.Shared
{
    public interface IOrderRepository
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.Shared.Ordering.Application.Ordering.PlaceOrder
{
    using DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.Shared.Ordering.Application.Shared;

    public sealed class PlaceOrderUseCase
    {
        private readonly IOrderRepository? _iOrderRepository;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.Shared.Ordering.Application.Fulfilment.ShipOrder
{
    using DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.Shared.Ordering.Application.Shared;

    public sealed class ShipOrderUseCase
    {
        private readonly IOrderRepository? _iOrderRepository;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.FlatCycle.Ordering.Application.PlaceOrder
{
    public sealed record PlaceOrderResult(string OrderId);
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.FlatCycle.Ordering.Application.PlaceOrder
{
    using DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.FlatCycle.Ordering.Application.ShipOrder;

    public sealed class PlaceOrderUseCase
    {
        private readonly ShipOrderResult? _shipOrderResult;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.FlatCycle.Ordering.Application.ShipOrder
{
    public sealed record ShipOrderResult(string Label);
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.FlatCycle.Ordering.Application.ShipOrder
{
    using DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.FlatCycle.Ordering.Application.PlaceOrder;

    public sealed class ShipOrderUseCase
    {
        private readonly PlaceOrderResult? _placeOrderResult;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.FlatOneWay.Ordering.Application.PlaceOrder
{
    public sealed record PlaceOrderResult(string OrderId);
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.FlatOneWay.Ordering.Application.PlaceOrder
{
    public sealed class PlaceOrderUseCase
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.FlatOneWay.Ordering.Application.ShipOrder
{
    using DomainCentric.ArchRules.Tests.Fixtures.Features.Slices.FlatOneWay.Ordering.Application.PlaceOrder;

    public sealed class ShipOrderUseCase
    {
        private readonly PlaceOrderResult? _placeOrderResult;
    }
}

