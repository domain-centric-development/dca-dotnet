using System.Collections.Generic;
using System.Linq;
using DomainCentric.ArchRules.Rules;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Rules;

public sealed class UseCaseRulesTests
{
    private const string Good = "DomainCentric.ArchRules.Tests.Fixtures.UseCase.Good";
    private const string Bad = "DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad";
    private const string Transactions = "DomainCentric.ArchRules.Tests.Fixtures.Transactions";

    private static readonly string[] ExpectedIds =
    {
        "DCA-USE-001",
        "DCA-USE-002",
        "DCA-USE-003",
        "DCA-USE-004",
        "DCA-USE-005",
        "DCA-USE-006",
        "DCA-USE-007",
        "DCA-USE-008",
        "DCA-USE-009",
        "DCA-USE-010",
        "DCA-USE-011",
        "DCA-USE-014",
        "DCA-USE-015",
    };

    /// <summary>Rules without a negative fixture (id → reason).</summary>
    private static readonly IReadOnlyDictionary<string, string> NoNegativeFixture = new Dictionary<string, string>();

    public static IEnumerable<object[]> RuleIds() => ExpectedIds.Select(id => new object[] { id });

    public static IEnumerable<object[]> NegativeRuleIds() =>
        ExpectedIds.Where(id => !NoNegativeFixture.ContainsKey(id)).Select(id => new object[] { id });

    private static DcaArchitecture Arch(string ns) =>
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(ns), typeof(UseCaseRulesTests).Assembly);

    private static IDcaRule Rule(string ns, string id) =>
        new UseCaseRules(DcaLayout.ForRootNamespace(ns)).Rules.Single(r => r.Id == id);

    [Fact]
    public void RuleSetHasStableShape()
    {
        var set = new UseCaseRules(DcaLayout.ForRootNamespace(Good));
        Assert.Equal("usecase", set.Name);
        Assert.Equal(ExpectedIds, set.Rules.Select(r => r.Id).ToArray());
        Assert.All(UseCaseRules.NotApplicable.Keys, id => Assert.DoesNotContain(id, ExpectedIds));
        Assert.All(set.Rules, r => Assert.False(string.IsNullOrWhiteSpace(r.Title)));
        Assert.All(set.Rules, r => Assert.False(string.IsNullOrWhiteSpace(r.Rationale)));
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

    [Fact]
    public void ResultRuleReportsTheTransitivePath()
    {
        var ex = Assert.Throws<DcaRuleViolationException>(() => Rule(Bad, "DCA-USE-015").Check(Arch(Bad)));
        Assert.Contains("ListOrdersResult.Orders : Order (IAggregateRoot)", ex.Message);
        Assert.Contains("ListOrdersResult.Latest -> OrderView.Order : Order (IAggregateRoot)", ex.Message);
        Assert.Contains("ListOrdersResult.FirstLine -> LineView.Line : OrderLine (IEntity)", ex.Message);
        Assert.Contains("ListOrdersResult.Parts -> OrderPart.Line : OrderLine (IEntity)", ex.Message);
        Assert.Contains("ListOrdersResult.Archive : Order (IAggregateRoot)", ex.Message);
        Assert.Contains("ListOrdersResult.Struct -> LinePart.Order : Order (IAggregateRoot)", ex.Message);
        Assert.Contains("ListOrdersResult.Boxed -> Boxed.Extra : Order (IAggregateRoot)", ex.Message);
    }

    /// <summary>The same part record reached through two members is reported on both paths.</summary>
    [Fact]
    public void ResultRuleReportsEveryPathThroughTheSamePartRecord()
    {
        var ex = Assert.Throws<DcaRuleViolationException>(() => Rule(Bad, "DCA-USE-015").Check(Arch(Bad)));
        Assert.Contains("ListOrdersResult.FirstLine -> LineView.Line : OrderLine (IEntity)", ex.Message);
        Assert.Contains("ListOrdersResult.LastLine -> LineView.Line : OrderLine (IEntity)", ex.Message);
    }

    /// <summary>A public instance member inherited from a base class without the suffix is part of the result.</summary>
    [Fact]
    public void ResultRuleIncludesInheritedMembers()
    {
        var ex = Assert.Throws<DcaRuleViolationException>(() => Rule(Bad, "DCA-USE-015").Check(Arch(Bad)));
        Assert.Contains("ArchivedOrdersResult.Pinned : Order (IAggregateRoot)", ex.Message);
    }

    /// <summary>
    /// <c>GenericBase&lt;T&gt;</c> declares <c>T Value</c>; the result binds <c>T</c>. The inherited member is read in the
    /// subclass's context, through every level of the hierarchy and inside containers bound to the parameter.
    /// </summary>
    [Fact]
    public void ResultRuleResolvesInheritedGenericMembers()
    {
        var ex = Assert.Throws<DcaRuleViolationException>(() => Rule(Bad, "DCA-USE-015").Check(Arch(Bad)));
        Assert.Contains("GenericOrderResult.Value : Order (IAggregateRoot)", ex.Message);
        Assert.Contains("BatchedOrdersResult.Value : Order (IAggregateRoot)", ex.Message);
        Assert.Contains("OrdersByRegionResult.Value : Order (IAggregateRoot)", ex.Message);
        Assert.Contains("LineItemResult.Value : OrderLine (IEntity)", ex.Message);
        Assert.DoesNotContain(".Count", ex.Message);
    }

    private string SaveRuleMessage() =>
        Assert.Throws<DcaRuleViolationException>(() => Rule(Transactions, "DCA-USE-009").Check(Arch(Transactions))).Message;

    [Fact]
    public void SaveRuleSharedHelperDoesNotConnectEntryMethods() =>
        Assert.Contains("SharedHelperUseCase.ExecuteAsync", SaveRuleMessage());

    [Fact]
    public void SaveRulePublishLoopIsReported() =>
        Assert.Contains("PublishLoopUseCase", SaveRuleMessage());

    [Fact]
    public void SaveRuleSplitHelpersPass() =>
        Assert.DoesNotContain("SplitHelpersUseCase", SaveRuleMessage());

    [Fact]
    public void SaveRuleFollowsMultiStepDelegation() =>
        Assert.DoesNotContain("MultiStepUseCase", SaveRuleMessage());

    [Fact]
    public void SaveRuleJudgesASharedSavingHelperPerEntryPath()
    {
        var message = SaveRuleMessage();
        Assert.Contains("SharedSaveHelperUseCase.ExecuteQuietlyAsync", message);
        Assert.DoesNotContain("SharedSaveHelperUseCase.ExecuteAsync ", message);
    }

    [Fact]
    public void SaveRuleTerminatesOnRecursion()
    {
        var message = SaveRuleMessage();
        Assert.DoesNotContain("RecursiveSaveUseCase", message);
        Assert.Contains("MutualRecursionUseCase.PingAsync", message);
    }

    [Fact]
    public void SaveRuleKeepsAPublicMethodAnEntryPointWhenAnotherMethodCallsIt()
    {
        var message = SaveRuleMessage();
        Assert.Contains("DirectEntryUseCase.ExecuteAsync", message);
        Assert.DoesNotContain("DirectEntryUseCase.CompleteAsync", message);
    }

    [Fact]
    public void SaveRuleIgnoresAPublicationInAnUnreachableMethod() =>
        Assert.Contains("SaveWithoutPublishUseCase.ExecuteAsync", SaveRuleMessage());
}
