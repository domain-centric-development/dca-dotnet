// Gateways fixture - DCA-TAC-027 fails: an aggregate that holds a domain gateway in a field, an entity that holds one
// in a type argument, a value that holds one in a static property read from a registry's field, a factory that looks
// one up from a registry, a specification that builds an implementation and returns it.
using System.Collections.Generic;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Gateways.Bad.Premiums.Domain.Gateway
{
    using DomainCentric.ArchRules.Tests.Fixtures.Gateways.Bad.Premiums.Domain.Model;

    public interface IRiskRates : IDomainGateway
    {
        RiskRate RateFor(Region region);
    }

    /// <summary>An implementation of the gateway, which the model must not build.</summary>
    public sealed class FixedRiskRates : IRiskRates
    {
        public RiskRate RateFor(Region region) => new(10);
    }

    /// <summary>A service locator: the form the model must not ask.</summary>
    public static class GatewayRegistry
    {
        public static readonly IRiskRates Rates = new FixedRiskRates();

        public static IRiskRates RiskRates() => Rates;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Gateways.Bad.Premiums.Domain.Model
{
    using DomainCentric.ArchRules.Tests.Fixtures.Gateways.Bad.Premiums.Domain.Gateway;

    public readonly record struct PolicyId(string Value) : IId;

    public sealed record Region(string Code) : IValue;

    public sealed record RiskRate(int PerMille) : IValue;

    /// <summary>Holds the gateway in a field.</summary>
    public sealed class Policy : AggregateRootBase<Policy, PolicyId>
    {
        private readonly IRiskRates rates;

        private Policy(PolicyId id, Region region, IRiskRates rates)
        {
            Id = id;
            Region = region;
            this.rates = rates;
        }

        public override PolicyId Id { get; }

        public Region Region { get; }

        public int Premium { get; private set; }

        public static Policy Open(PolicyId id, Region region, IRiskRates rates) => new(id, region, rates);

        public void Price() => Premium = rates.RateFor(Region).PerMille;
    }

    /// <summary>Holds the gateway, hidden in a type argument.</summary>
    public sealed class Cover : IEntity<Cover, PolicyId>
    {
        private readonly IReadOnlyList<IRiskRates> sources;

        private Cover(PolicyId id, IReadOnlyList<IRiskRates> sources)
        {
            Id = id;
            this.sources = sources;
        }

        public PolicyId Id { get; }

        public int SourceCount => sources.Count;
    }

    /// <summary>Holds the gateway in a static property, filled from another type's static field.</summary>
    public sealed record Premium(int PerMille) : IValue
    {
        private static IRiskRates Rates { get; } = GatewayRegistry.Rates;

        public static Premium In(Region region) => new(Rates.RateFor(region).PerMille);
    }

    /// <summary>Looks the gateway up from a registry.</summary>
    public sealed class PolicyFactory : IFactory
    {
        public Premium Quote(Region region) => new(GatewayRegistry.RiskRates().RateFor(region).PerMille);
    }

    /// <summary>Builds a gateway implementation and hands it out.</summary>
    public sealed class InsurableRegion : ISpecification<Region>
    {
        public IRiskRates Rates() => new FixedRiskRates();

        public bool IsSatisfiedBy(Region candidate) => Rates().RateFor(candidate).PerMille < 100;
    }
}
