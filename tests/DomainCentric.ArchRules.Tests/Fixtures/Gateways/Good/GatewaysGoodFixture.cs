// Gateways fixture - DCA-TAC-027 passes: an aggregate and a value take the domain gateway as a parameter and call it
// (the value from a closure), a domain service and a use case call it.
using System;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Gateways.Good.Premiums.Domain.Gateway
{
    using DomainCentric.ArchRules.Tests.Fixtures.Gateways.Good.Premiums.Domain.Model;

    public interface IRiskRates : IDomainGateway
    {
        RiskRate RateFor(Region region);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Gateways.Good.Premiums.Domain.Model
{
    using DomainCentric.ArchRules.Tests.Fixtures.Gateways.Good.Premiums.Domain.Gateway;

    public readonly record struct PolicyId(string Value) : IId;

    public sealed record Region(string Code) : IValue;

    public sealed record RiskRate(int PerMille) : IValue;

    /// <summary>Calls a gateway it receives as a parameter.</summary>
    public sealed record Premium(int PerMille) : IValue
    {
        public static Premium In(Region region, IRiskRates rates)
        {
            Func<int> lookup = () => rates.RateFor(region).PerMille;
            return new Premium(lookup());
        }
    }

    /// <summary>Takes the fact, or the gateway itself as a parameter.</summary>
    public sealed class Policy : AggregateRootBase<Policy, PolicyId>
    {
        private Policy(PolicyId id, Region region)
        {
            Id = id;
            Region = region;
        }

        public override PolicyId Id { get; }

        public Region Region { get; }

        public int Premium { get; private set; }

        public static Policy Open(PolicyId id, Region region) => new(id, region);

        public void Price(RiskRate rate) => Premium = rate.PerMille;

        public void Reprice(IRiskRates rates) => Premium = Domain.Model.Premium.In(Region, rates).PerMille;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Gateways.Good.Premiums.Domain.Service
{
    using DomainCentric.ArchRules.Tests.Fixtures.Gateways.Good.Premiums.Domain.Gateway;
    using DomainCentric.ArchRules.Tests.Fixtures.Gateways.Good.Premiums.Domain.Model;

    /// <summary>A domain service calls the gateway and hands the fact to the aggregate.</summary>
    public sealed class PolicyPricing : IDomainService
    {
        private readonly IRiskRates rates;

        public PolicyPricing(IRiskRates rates) => this.rates = rates;

        public void Price(Policy policy) => policy.Price(rates.RateFor(policy.Region));
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Gateways.Good.Premiums.Application.UseCase
{
    using DomainCentric.ArchRules.Tests.Fixtures.Gateways.Good.Premiums.Domain.Gateway;
    using DomainCentric.ArchRules.Tests.Fixtures.Gateways.Good.Premiums.Domain.Model;

    /// <summary>The application calls the gateway too.</summary>
    public sealed class QuotePremiumService
    {
        private readonly IRiskRates rates;

        public QuotePremiumService(IRiskRates rates) => this.rates = rates;

        public int Quote(string region)
        {
            var policy = Policy.Open(new PolicyId("p-1"), new Region(region));
            policy.Price(rates.RateFor(policy.Region));
            return policy.Premium;
        }
    }
}
