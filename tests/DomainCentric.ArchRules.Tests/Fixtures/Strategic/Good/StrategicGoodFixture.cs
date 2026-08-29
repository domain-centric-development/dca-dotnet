// Strategic fixture — every DCA-STR rule passes here.
using System;
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Good.SharedKernel
{
    [SharedKernel(Description = "Shared value objects")]
    public static class SharedKernelContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Good.SharedKernel.Domain.Model
{
    public sealed record Money(decimal Amount, string Currency) : IValue;
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Good.Catalog
{
    [BoundedContext("Catalog", Description = "Product catalog")]
    public static class CatalogContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Good.Catalog.Domain.Model
{
    using SharedKernel.Domain.Model;

    public sealed class Product
    {
        public Product(Guid id, Money price)
        {
            Id = id;
            Price = price;
        }

        public Guid Id { get; }

        public Money Price { get; }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Good.Catalog.Application.FindProduct
{
    using Domain.Model;

    public sealed class FindProductUseCase
    {
        public Product? Execute(Guid id) => null;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Good.Catalog.Api
{
    public sealed record ProductInfo(Guid Id, decimal Price);

    [OpenHostService("Catalog", Description = "Product lookup for other contexts")]
    public interface ICatalogApi
    {
        ProductInfo? Find(Guid id);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Good.Catalog.Events
{
    public sealed record ProductAddedEvent(Guid EventId, Guid ProductId, DateTimeOffset OccurredOn) : IIntegrationEvent;
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Good.Cart
{
    [BoundedContext("Cart", Description = "Shopping cart")]
    public static class CartContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Good.Cart.Domain.Model
{
    using SharedKernel.Domain.Model;

    public sealed class Cart
    {
        public Money Total { get; private set; } = new(0m, "EUR");

        public void Add(Money price) => Total = new Money(Total.Amount + price.Amount, Total.Currency);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Good.Cart.Application.AddToCart
{
    using Domain.Model;

    public sealed class AddToCartUseCase
    {
        public Cart Execute(Cart cart) => cart;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Good.Cart.Adapter.Outgoing.Catalog
{
    using Good.Catalog.Api;

    public sealed class CatalogAdapter
    {
        private readonly ICatalogApi _api;

        public CatalogAdapter(ICatalogApi api)
        {
            _api = api;
        }

        public decimal? PriceOf(Guid id) => _api.Find(id)?.Price;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Strategic.Good.Cart.Adapter.Incoming.Event.Acl
{
    using Good.Catalog.Events;

    public sealed class ProductEventTranslator
    {
        public Guid Translate(ProductAddedEvent e) => e.ProductId;
    }
}
