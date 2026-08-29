// Strategic fixture — one violation per DCA-STR rule (002..009).
using System;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Bad.SharedKernel
{
    [SharedKernel]
    public static class SharedKernelContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Bad.SharedKernel.Domain.Model
{
    // DCA-STR-002: shared kernel depends on a bounded context
    public sealed record Money(decimal Amount, Bad.Catalog.Domain.Model.Product For) : IValue;
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Bad.Catalog
{
    [BoundedContext("Catalog")]
    public static class CatalogContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Bad.Catalog.Domain.Model
{
    public sealed class Product
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    // DCA-STR-005: Open Host Service outside Api / Adapter.Incoming.OpenHost
    [OpenHostService("Catalog")]
    public sealed class CatalogService
    {
        public Product? Find(Guid id) => null;
    }

    // DCA-STR-007: integration event outside Events / Adapter.Outgoing.Event
    public sealed record ProductAddedEvent(Guid EventId, DateTimeOffset OccurredOn) : IIntegrationEvent;
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Bad.Catalog.Events
{
    // DCA-STR-008: integration event that is not a record
    public sealed class ProductRemovedEvent : IIntegrationEvent
    {
        public Guid EventId { get; set; }

        public DateTimeOffset OccurredOn { get; set; }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Bad.Catalog.Application.FindProduct
{
    using Domain.Model;

    public sealed class FindProductUseCase
    {
        public Product? Execute(Guid id) => null;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Bad.Cart
{
    [BoundedContext("Cart")]
    public static class CartContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Bad.Cart.Domain.Model
{
    // DCA-STR-004: domain layer depends on another context
    public sealed class Cart
    {
        public Bad.Catalog.Domain.Model.Product? LastAdded { get; set; }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Bad.Cart.Application.AddToCart
{
    // DCA-STR-003: application layer depends on another context
    public sealed class AddToCartUseCase
    {
        public Bad.Catalog.Domain.Model.Product? Execute(Guid id) => null;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Bad.Cart.Adapter.Outgoing.Catalog
{
    // DCA-STR-006: outgoing adapter reaches into another context's domain and application layer
    public sealed class CatalogAdapter
    {
        private readonly Bad.Catalog.Application.FindProduct.FindProductUseCase _useCase = new();

        public Bad.Catalog.Domain.Model.Product? Find(Guid id) => _useCase.Execute(id);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Bad.Cart.Adapter.Incoming.Event
{
    // DCA-STR-009: ACL component outside an Acl namespace
    public sealed class ProductEventTranslator
    {
        public Guid Translate(Bad.Catalog.Events.ProductRemovedEvent e) => e.EventId;
    }
}
