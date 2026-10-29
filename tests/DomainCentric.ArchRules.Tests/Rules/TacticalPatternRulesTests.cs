using System;
using System.Collections.Generic;
using System.Linq;
using DomainCentric.ArchRules.Rules;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Rules;

public sealed class TacticalPatternRulesTests
{
    private const string Good = "DomainCentric.ArchRules.Tests.Fixtures.Tactical.Good";
    private const string Bad = "DomainCentric.ArchRules.Tests.Fixtures.Tactical.Bad";
    private const string Statics = "DomainCentric.ArchRules.Tests.Fixtures.Tactical.Statics";

    private static readonly IReadOnlyDictionary<string, string> NoNegativeFixture = new Dictionary<string, string>
    {
        ["DCA-TAC-025"] = "negative fixture in Primitives - BarePrimitivesInTheDomainAreReported",
    };

    /// <summary>Experimental and stricter than the shared good fixture; its positive fixture is Primitives.Good.</summary>
    private static readonly string[] NotOnTheSharedGoodFixture = { "DCA-TAC-025" };

    private static readonly string[] ExpectedIds = Enumerable.Range(1, 21).Select(i => $"DCA-TAC-{i:000}").Append("DCA-TAC-023").Append("DCA-TAC-024").Append("DCA-TAC-025").ToArray();

    public static IEnumerable<object[]> RuleIds() =>
        ExpectedIds.Where(id => !NotOnTheSharedGoodFixture.Contains(id)).Select(id => new object[] { id });

    public static IEnumerable<object[]> NegativeRuleIds() =>
        ExpectedIds.Where(id => !NoNegativeFixture.ContainsKey(id)).Select(id => new object[] { id });

    private static DcaArchitecture Arch(string ns) =>
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(ns), typeof(TacticalPatternRulesTests).Assembly);

    private static IDcaRule Rule(string ns, string id) =>
        new TacticalPatternRules(DcaLayout.ForRootNamespace(ns)).Rules.Single(r => r.Id == id);

    [Fact]
    public void RuleSetHasStableShape()
    {
        var set = new TacticalPatternRules(DcaLayout.ForRootNamespace(Good));
        Assert.Equal("tactical", set.Name);
        Assert.Equal(ExpectedIds, set.Rules.Select(r => r.Id).ToArray());
        Assert.Empty(TacticalPatternRules.NotApplicable.Keys.Intersect(set.Rules.Select(r => r.Id)));
        Assert.All(set.Rules, r => Assert.False(string.IsNullOrWhiteSpace(r.Title) || string.IsNullOrWhiteSpace(r.Rationale)));
    }

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void GoodFixturePasses(string id) => Rule(Good, id).Check(Arch(Good));

    [Theory]
    [MemberData(nameof(NegativeRuleIds))]
    public void BadFixtureFails(string id)
    {
        var ex = Assert.Throws<DcaRuleViolationException>(() => Rule(Bad, id).Check(Arch(Bad)));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    /// <summary>
    /// A static port breaks persistence ignorance (TAC-002); a static member is not instance state, so
    /// TAC-003, TAC-007 and TAC-008 all pass over one. The three read the same thing, in both languages.
    /// </summary>
    [Fact]
    public void StaticMembersArePortsButNotAggregateOrEntityState()
    {
        var message = Assert.Throws<DcaRuleViolationException>(() => Rule(Statics, "DCA-TAC-002").Check(Arch(Statics))).Message;
        Assert.Contains("Lookup", message, System.StringComparison.Ordinal);
        foreach (var id in new[] { "DCA-TAC-003", "DCA-TAC-007", "DCA-TAC-008" })
        {
            Rule(Statics, id).Check(Arch(Statics));
        }
    }

    [Theory]
    [InlineData("DCA-TAC-003", "Order", "LinkedOrder")]
    [InlineData("DCA-TAC-003", "Order", "SuppliedOrder")]
    [InlineData("DCA-TAC-007", "Shipment", "LinkedOrder")]
    [InlineData("DCA-TAC-008", "ReferenceValue", "Order")]
    public void MarkerInterfacesAndWrappersCannotHideIdentities(string id, string owner, string member)
    {
        var message = Assert.Throws<DcaRuleViolationException>(() => Rule(Bad, id).Check(Arch(Bad))).Message;
        Assert.Contains(owner + " has field '" + member + "'", message);
    }

    /// <summary>
    /// A container of the aggregate's own type holds <em>other</em> instances of that aggregate. Direct references and containers are both rejected.
    /// </summary>
    [Fact]
    public void ContainersOfTheOwnAggregateTypeAreReported()
    {
        var message = Assert.Throws<DcaRuleViolationException>(() => Rule(Bad, "DCA-TAC-003").Check(Arch(Bad))).Message;
        foreach (var container in new[] { "Children", "Siblings", "ByName", "Tree" })
        {
            Assert.Matches($"Category has field '{container}' containing .*Category", message);
        }

        Assert.Contains("'Root'", message);
        Assert.Matches("Order has field 'Customer' of type .*Customer", message);
    }

    /// <summary>
    /// The aggregate comes from the bound type argument, and the name must name it. A repository
    /// bound to one aggregate and named after another is what name-only resolution could not see;
    /// a generic intermediate port binds a type variable and is skipped instead of misreported.
    /// </summary>
    [Fact]
    public void PublicConstructorsOfAggregateRootsAreReported()
    {
        var message = Assert.Throws<DcaRuleViolationException>(() => Rule(Bad, "DCA-TAC-023").Check(Arch(Bad))).Message;
        Assert.Contains("Tactical.Bad.Ordering.Domain.Model.Order declares public constructor", message, StringComparison.Ordinal);
        Assert.Contains("Reconstitute", message, StringComparison.Ordinal);
    }

    [Fact]
    public void RepositoriesAreNamedAfterTheAggregateTheyBind()
    {
        var message = Assert.Throws<DcaRuleViolationException>(() => Rule(Bad, "DCA-TAC-016").Check(Arch(Bad))).Message;
        Assert.Contains("IShipmentRepository binds", message, StringComparison.Ordinal);
        Assert.Contains("name it OrderRepository", message, StringComparison.Ordinal);
        Assert.DoesNotContain("IAuditedRepository", message, StringComparison.Ordinal);

        Rule(Good, "DCA-TAC-016").Check(Arch(Good));
    }

    [Fact]
    public void ANeutralFactoryBesideAnOriginFactoryIsReportedAndTwoOriginFactoriesPass()
    {
        var message = Assert.Throws<DcaRuleViolationException>(() => Rule(Bad, "DCA-TAC-024").Check(Arch(Bad))).Message;

        Assert.Contains("Code declares the neutral factory Of(System.String) beside FromHash with the same parameters", message);
        Assert.DoesNotContain("Label", message);
    }

    private const string PrimitivesBad = "DomainCentric.ArchRules.Tests.Fixtures.Primitives.Bad";
    private const string PrimitivesGood = "DomainCentric.ArchRules.Tests.Fixtures.Primitives.Good";

    [Fact]
    public void BarePrimitivesInTheDomainAreReported()
    {
        var message = Assert.Throws<DcaRuleViolationException>(
            () => Rule(PrimitivesBad, "DCA-TAC-025").Check(Arch(PrimitivesBad))).Message;

        Assert.DoesNotContain("IsOverdrawn", message);
        Assert.Contains("field _overdraftCount is Int32", message);
        Assert.Contains("property Closed is Boolean", message);
        Assert.Contains("property OpenedAt is DateTimeOffset", message);
        Assert.Contains("property Tags is String", message);
        Assert.Contains("property OwnerId is Guid", message);
        Assert.Contains("Rename(...) takes String as parameter 1", message);
        Assert.Contains("Address: property City is String; property Street is String", message);
        Assert.Contains("AccountOpened: property Holder is String", message);
        Assert.DoesNotContain("OccurredOn", message);
        Assert.DoesNotContain("PostalCode:", message);
        Assert.DoesNotContain("AccountId:", message);
        Assert.DoesNotContain("ToString", message);
    }

    [Fact]
    public void ADomainOfValueObjectsPasses() => Rule(PrimitivesGood, "DCA-TAC-025").Check(Arch(PrimitivesGood));
}
