using Acme.Shop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace Acme.Shop.Cart.Domain.Model;

public sealed record CartItem(ProductId ProductId, Quantity Quantity) : IValue;
