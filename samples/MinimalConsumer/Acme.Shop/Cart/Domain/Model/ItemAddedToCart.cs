using Acme.Shop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace Acme.Shop.Cart.Domain.Model;

public sealed record ItemAddedToCart(Guid EventId, DateTimeOffset OccurredOn, CartId CartId, ProductId ProductId) : IDomainEvent;
