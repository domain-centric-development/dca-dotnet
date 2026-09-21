using System;
using System.Linq;
using Xunit;

namespace DomainCentric.ArchRules.Tests;

/// <summary>
/// The rules select on the configured roles, not on the library's own types: a code base with its own
/// vocabulary is governed once the roles name its markers, and is invisible to the rules until then.
/// </summary>
public sealed class DcaMarkersTests
{
    private const string Fixture = "DomainCentric.ArchRules.Tests.Fixtures.OwnMarkers";
    private const string Vocabulary = Fixture + ".Vocabulary";

    /// <summary>The same vocabulary, in a tree whose domain failure sits in an incoming adapter.</summary>
    private const string Misplaced = "DomainCentric.ArchRules.Tests.Fixtures.OwnMarkersBad";

    private static readonly DcaMarkers Own = DcaMarkers.Dca() with
    {
        Name = "own",
        AggregateRoot = Vocabulary + ".IContractRoot",
        Repository = Vocabulary + ".IContractStore",
        DomainException = Vocabulary + ".ContractFailure",
    };

    private static DcaArchitecture Arch(DcaMarkers markers) => Arch(Fixture, markers);

    private static DcaArchitecture Arch(string ns, DcaMarkers markers) =>
        DcaArchitecture.Load(
            DcaLayout.ForRootNamespace(ns).WithMarkers(markers),
            typeof(DcaMarkersTests).Assembly);

    private static IDcaRule Rule(DcaMarkers markers, string id) => Rule(Fixture, markers, id);

    private static IDcaRule Rule(string ns, DcaMarkers markers, string id) =>
        DcaRules.All(DcaLayout.ForRootNamespace(ns).WithMarkers(markers)).Single(r => r.Id == id);

    [Fact]
    public void TheDefaultIsTheLibrarysVocabulary()
    {
        Assert.True(DcaMarkers.Dca().IsDefault);
        Assert.Equal("dca", DcaMarkers.Dca().Name);
        Assert.Equal(
            DcaMarkers.DcaBuildingBlocks + ".Hexagonal.Ports.Out.IRepository",
            DcaMarkers.Dca().Repository);
    }

    [Fact]
    public void OwnVocabularyIsNotSelectedByTheDefaultRoles()
    {
        Rule(DcaMarkers.Dca(), "DCA-TAC-002").Check(Arch(DcaMarkers.Dca()));
    }

    [Fact]
    public void OwnVocabularyIsGovernedOnceTheRolesNameIt()
    {
        var arch = Arch(Own);

        var message = Assert
            .Throws<DcaRuleViolationException>(() => Rule(Own, "DCA-TAC-002").Check(arch))
            .Message;

        Assert.Contains("TariffContract", message);
        Assert.Contains("ITariffContractStore", message);
        Assert.False(Own.IsDefault);
    }

    [Theory]
    [InlineData("DCA-ERR-001")]
    [InlineData("DCA-ERR-002")]
    [InlineData("DCA-ERR-003")]
    [InlineData("DCA-ERR-004")]
    [InlineData("DCA-ERR-005")]
    public void TheErrorRulesFollowTheConfiguredBaseType(string id)
    {
        Rule(Own, id).Check(Arch(Own));
    }

    [Fact]
    public void AMarkerReachedThroughABaseTypeIsSelected()
    {
        var roles = DcaMarkers.Dca() with
        {
            Name = "own",
            DomainException = Misplaced + ".Vocabulary.ContractFailure",
        };

        var message = Assert
            .Throws<DcaRuleViolationException>(
                () => Rule(Misplaced, roles, "DCA-ERR-002").Check(Arch(Misplaced, roles)))
            .Message;

        Assert.Contains("TariffRejectedException", message);
        Assert.Contains("domain layer", message);
    }

    [Fact]
    public void TheReportNamesTheVocabulary()
    {
        Assert.Equal("dca (library default)", DcaLayout.ForRootNamespace(Fixture).MarkersReport());

        var report = DcaLayout.ForRootNamespace(Fixture).WithMarkers(Own).MarkersReport();

        Assert.StartsWith("own (", report);
        Assert.Contains("AggregateRoot=" + Vocabulary + ".IContractRoot", report);
        Assert.DoesNotContain("Entity=", report);
    }

    [Fact]
    public void ABlankVocabularyNameIsRefused()
    {
        var refused = Assert.Throws<ArgumentException>(
            () => (DcaMarkers.Dca() with { Name = " " }).Validate());

        Assert.Contains("Name", refused.Message);
    }

    [Fact]
    public void AnEmptyRoleIsRefused()
    {
        var refused = Assert.Throws<ArgumentException>(
            () => (DcaMarkers.Dca() with { Repository = "" }).Validate());

        Assert.Contains("Repository", refused.Message);
    }
}
