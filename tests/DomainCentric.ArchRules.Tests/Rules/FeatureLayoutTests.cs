using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Rules;

/// <summary>
/// Features — optional, domain-named groups of use cases below a module's application namespace
/// (<c>Application.&lt;Feature&gt;.&lt;UseCase&gt;</c>) — and the two rules that keep them legible:
/// <c>DCA-USE-014</c> (one consistent use-case depth per module) and <c>DCA-CYC-005</c> (no cycles between the
/// feature or use-case slices of one module). The compatibility fixture pins that a grouped context is governed
/// by the pre-existing catalog exactly like a flat one.
/// </summary>
public sealed class FeatureLayoutTests
{
    private const string Fixtures = "DomainCentric.ArchRules.Tests.Fixtures.Features";

    private static DcaArchitecture Arch(string ns) =>
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(ns), typeof(FeatureLayoutTests).Assembly);

    private static IDcaRule Rule(string ns, string id) =>
        DcaRules.All(DcaLayout.ForRootNamespace(ns)).Single(r => r.Id == id);

    private static List<string> Failures(string ns)
    {
        var arch = Arch(ns);
        var messages = new List<string>();
        foreach (var rule in DcaRules.All(arch.Layout))
        {
            try
            {
                rule.Check(arch);
            }
            catch (DcaRuleViolationException e)
            {
                messages.Add(rule.Id + " :: " + e.Message.Replace('\n', ' '));
            }
        }

        return messages;
    }

    public sealed class Compatibility
    {
        private const string Compat = Fixtures + ".Compat";

        [Fact]
        public void TheGroupedContextIsOneModuleAndOneBoundedContext()
        {
            var arch = Arch(Compat);
            Assert.Equal(new[] { Compat + ".Sales" }, arch.IsolatedModuleRoots());
            Assert.Contains(Compat + ".Sales", arch.BoundedContexts.Keys);
        }

        [Fact]
        public void TheWholeCatalogPasses() => Assert.Empty(Failures(Compat));

        [Fact]
        public void TheFeatureNamespaceIsSeenByTheApplicationSelectors()
        {
            var arch = Arch(Compat);
            Assert.Single(arch.AllApplicationPatterns());
            Assert.Contains(
                arch.Classes,
                c => c.Namespace?.FullName.EndsWith(".Application.Ordering.PlaceOrder", System.StringComparison.Ordinal) == true
                    && c.Name == "PlaceOrderUseCase");
        }
    }

    public sealed class UseCaseDepth
    {
        private const string Depth = Fixtures + ".Depth";
        private const string Id = "DCA-USE-014";

        private static void Passes(string shape) => Rule(Depth + "." + shape, Id).Check(Arch(Depth + "." + shape));

        private static DcaRuleViolationException Fails(string shape) =>
            Assert.Throws<DcaRuleViolationException>(() => Rule(Depth + "." + shape, Id).Check(Arch(Depth + "." + shape)));

        [Fact]
        public void FlatLayoutPasses() => Passes("Flat");

        [Fact]
        public void GroupedLayoutPasses() => Passes("Grouped");

        [Fact]
        public void AnAbstractBaseClassNamedLikeAUseCaseDoesNotCount()
        {
            // both fixtures carry Application.Support.BaseUseCase — depth 2 in the flat layout, an extra
            // namespace in the grouped one; neither may turn into a mixed-depth violation
            Passes("Flat");
            Passes("Grouped");
        }

        [Fact]
        public void ASingleUseCaseMayUseEitherDepth()
        {
            Passes("Single");
            Passes("SingleFlat");
        }

        [Fact]
        public void AModuleWithoutUseCasesIsValid() => Passes("NoUseCase");

        [Fact]
        public void AUseCaseDirectlyInTheApplicationNamespaceIsReported()
        {
            var e = Fails("Shallow");
            Assert.Single(e.Violations);
            Assert.Contains("directly in the application namespace", e.Violations[0]);
        }

        [Fact]
        public void AUseCaseNestedDeeperThanAFeatureIsReported()
        {
            var e = Fails("OverDeep");
            Assert.Single(e.Violations);
            Assert.Contains("nested deeper", e.Violations[0]);
            Assert.Contains("Application.Ordering.Placement.PlaceOrder", e.Violations[0]);
        }

        [Fact]
        public void MixingBothFormsIsReportedOnceNamingBoth()
        {
            var e = Fails("Mixed");
            Assert.Single(e.Violations);
            Assert.Contains("mixes flat use case namespaces", e.Violations[0]);
            Assert.Contains("Application.PlaceOrder", e.Violations[0]);
            Assert.Contains("Application.Fulfilment.ShipOrder", e.Violations[0]);
            Assert.Contains(Depth + ".Mixed.Ordering", e.Violations[0]);
        }
    }

    public sealed class ApplicationSlices
    {
        private const string Slices = Fixtures + ".Slices";
        private const string Id = "DCA-CYC-005";

        private static void Passes(string shape) => Rule(Slices + "." + shape, Id).Check(Arch(Slices + "." + shape));

        private static DcaRuleViolationException Fails(string shape) =>
            Assert.Throws<DcaRuleViolationException>(() => Rule(Slices + "." + shape, Id).Check(Arch(Slices + "." + shape)));

        [Fact]
        public void IndependentFeaturesPass() => Passes("Independent");

        [Fact]
        public void AOneDirectionalDependencyPasses() => Passes("OneWay");

        [Fact]
        public void ADependencyOnApplicationSharedIsNotASliceDependency() => Passes("Shared");

        [Fact]
        public void ACycleBetweenTwoFeaturesNamesBothSlices()
        {
            var message = Fails("Cycle").Message;
            Assert.Contains("Application.Ordering", message);
            Assert.Contains("Application.Fulfilment", message);
        }

        [Fact]
        public void FlatUseCaseNamespacesAreSlicedTheSameWay()
        {
            Passes("FlatOneWay");
            var message = Fails("FlatCycle").Message;
            Assert.Contains("Application.PlaceOrder", message);
            Assert.Contains("Application.ShipOrder", message);
        }
    }
}
