using System;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Naming.Bad.Ordering.Domain.Model;

public readonly record struct OrderId(Guid Value) : IId;
