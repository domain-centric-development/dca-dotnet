using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DomainCentric.ArchRules.Tests.Core;

/// <summary>
/// Context discovery is by <c>[BoundedContext]</c>, not by position in the namespace tree, so a context
/// may sit at any depth. These fixtures cover the layouts that the former one-segment wildcard
/// (<c>Root.[^.]+.Domain</c>) made impossible, and pin the failure mode that made the loss invisible:
/// a context nested one level too deep used to pass every layer rule for lack of subjects.
/// </summary>
public sealed class ContextDiscoveryTests
{
    private const string Fixtures = "DomainCentric.ArchRules.Tests.Fixtures.Layout";

    private static DcaArchitecture Arch(string root) =>
        DcaArchitecture.Load(DcaLayout.ForRootNamespace(root), typeof(ContextDiscoveryTests).Assembly);

    private static IDcaRule Rule(string id, string root) =>
        DcaRules.All(DcaLayout.ForRootNamespace(root)).Single(r => r.Id == id);

    private static void CheckWholeCatalog(string root)
    {
        var arch = Arch(root);
        foreach (var rule in DcaRules.All(DcaLayout.ForRootNamespace(root)))
        {
            rule.Check(arch);
        }
    }

    public sealed class Grouped
    {
        private const string Root = Fixtures + ".Grouped";

        [Fact]
        public void IsDiscoveredAtDepthTwo() =>
            Assert.Equal(new[] { Root + ".Sales.Order" }, Arch(Root).BoundedContextNamespaces);

        [Fact]
        public void IsNamedByItsNamespaceRelativeToTheRoot() =>
            Assert.Equal("Sales.Order", Arch(Root).ContextName(Root + ".Sales.Order"));

        [Fact]
        public void ResolvesTypesToTheDeclaredAncestorAndNotToTheGroup() =>
            Assert.Equal(Root + ".Sales.Order", Arch(Root).RootContextNamespace(Root + ".Sales.Order.Domain.Model"));

        [Fact]
        public void IsGovernedByTheLayerRules()
        {
            var ex = Assert.Throws<DcaRuleViolationException>(() => Rule("DCA-LAY-002", Root).Check(Arch(Root)));
            Assert.Contains("Order", ex.Message, System.StringComparison.Ordinal);
        }

        /// <summary>A namespace named Domain inside an adapter stays part of its context.</summary>
        [Fact]
        public void KeepsADomainNamedAdapterNamespaceInsideItsContext() =>
            Assert.Equal(Root + ".Sales.Order", Arch(Root).RootContextNamespace(Root + ".Sales.Order.Adapter.Outgoing.Domain"));

        /// <summary>The web-adapter namespace of a grouped context is derived from its module root.</summary>
        [Fact]
        public void ItsViewModelsAreInTheRightPlace() => Rule("DCA-NAM-011", Root).Check(Arch(Root));
    }

    public sealed class Flat
    {
        private const string Root = Fixtures + ".Flat";

        [Fact]
        public void IsDiscoveredAsItsOwnContext() =>
            Assert.Equal(new[] { Root }, Arch(Root).BoundedContextNamespaces);

        [Fact]
        public void IsNamedByTheRootsLastSegment() =>
            Assert.Equal("Flat", Arch(Root).ContextName(Root));

        [Fact]
        public void IsGovernedByTheLayerRules() =>
            Assert.Throws<DcaRuleViolationException>(() => Rule("DCA-LAY-002", Root).Check(Arch(Root)));

        /// <summary><c>Root.Adapter.Incoming.Web</c> is a web-adapter namespace when the root is the context.</summary>
        [Fact]
        public void ItsViewModelsAreInTheRightPlace() => Rule("DCA-NAM-011", Root).Check(Arch(Root));
    }

    public sealed class Undeclared
    {
        private const string Root = Fixtures + ".Nested";

        [Fact]
        public void IsNotDiscovered() => Assert.Empty(Arch(Root).BoundedContexts);

        /// <summary>
        /// The layer rules select structurally, over <see cref="DcaArchitecture.ModuleRoots"/>, so a context
        /// nested one level too deep is governed even before anyone declares it: <c>Todo</c> depends on
        /// infrastructure and DCA-LAY-002 says so. Under the one-segment wildcard the rule found no types at all.
        /// </summary>
        [Fact]
        public void IsStillGovernedByTheLayerRules()
        {
            var ex = Assert.Throws<DcaRuleViolationException>(() => Rule("DCA-LAY-002", Root).Check(Arch(Root)));
            Assert.Contains("Todo", ex.Message, System.StringComparison.Ordinal);
        }

        [Fact]
        public void IsFoundAsAModuleRootDespiteItsDepth() =>
            Assert.Equal(new[] { Root + ".Contexts.Todo" }, Arch(Root).ModuleRoots());

        /// <summary>Isolation is structural too: no declaration needed to be a subject of the isolation rules.</summary>
        [Fact]
        public void IsASubjectOfTheIsolationRules() =>
            Assert.Equal(new[] { Root + ".Contexts.Todo" }, Arch(Root).IsolatedModuleRoots());

        [Fact]
        public void FallsBackToTheFirstSegmentWhenNothingIsDeclared() =>
            Assert.Equal(Root + ".Contexts", Arch(Root).RootContextNamespace(Root + ".Contexts.Todo.Domain.Model"));
    }

    public sealed class GroupedModule
    {
        private const string Root = Fixtures + ".GroupedModule";

        [Fact]
        public void IsNoBoundedContext() => Assert.Empty(Arch(Root).BoundedContexts);

        [Fact]
        public void IsFoundAsAModuleRootAtDepthTwo() =>
            Assert.Equal(new[] { Root + ".Generic.Backoffice" }, Arch(Root).ModuleRoots());

        /// <summary>Its layers are governed although it declares no bounded context.</summary>
        [Fact]
        public void ContributesItsLayerPatterns() =>
            Assert.Equal(new[] { DcaLayout.Below(Root + ".Generic.Backoffice.Application") }, Arch(Root).AllApplicationPatterns());

        /// <summary>
        /// DCA requires no declaration from it: it is a subject of the isolation rules as it stands, and
        /// none of them asks it to declare what it is.
        /// </summary>
        [Fact]
        public void IsIsolatedWithoutDeclaringAnything()
        {
            var arch = Arch(Root);
            Assert.Equal(new[] { Root + ".Generic.Backoffice" }, arch.IsolatedModuleRoots());
            foreach (var id in new[] { "DCA-STR-003", "DCA-STR-004", "DCA-STR-006", "DCA-HEX-007" })
            {
                Rule(id, Root).Check(arch);
            }
        }
    }

    public sealed class EmptyContext
    {
        private const string Root = Fixtures + ".EmptyContext";

        /// <summary>
        /// A context is a boundary of language and ownership, not a file count — declaring it before the
        /// model exists is legitimate, and discovery must see it so that it appears on the context map.
        /// </summary>
        [Fact]
        public void IsDiscovered() => Assert.Equal(new[] { Root + ".Planned" }, Arch(Root).BoundedContextNamespaces);

        /// <summary>It owns no layer yet, so it is no module root and the layer rules have nothing to say.</summary>
        [Fact]
        public void OwnsNoModuleYet() => Assert.Empty(Arch(Root).ModuleRoots());
    }

    public sealed class TransactionScript
    {
        private const string Root = Fixtures + ".TransactionScript";

        /// <summary>
        /// A bounded context is a boundary of language; which tactical patterns live inside it is a
        /// separate decision, taken per subdomain. A generic or supporting subdomain may legitimately be
        /// a transaction script — a use case over a store, no aggregate, no Domain namespace at all.
        /// </summary>
        [Fact]
        public void IsADeclaredContextWithoutAnyDomainNamespace()
        {
            var arch = Arch(Root);
            Assert.Equal(new[] { Root + ".Reporting" }, arch.BoundedContextNamespaces);
            Assert.Equal(new[] { Root + ".Reporting" }, arch.ModuleRoots());
            Assert.DoesNotContain(arch.Types, t => t.Namespace.FullName.Contains(".Domain", System.StringComparison.Ordinal));
        }

        /// <summary>The whole catalog, with no configuration, against a context with no domain model.</summary>
        [Fact]
        public void PassesTheWholeCatalog() => CheckWholeCatalog(Root);
    }

    public sealed class GroupedCycle
    {
        private const string Root = Fixtures + ".GroupedCycle";

        [Fact]
        public void BothContextsAreDiscoveredAtDepthTwo() =>
            Assert.Equal(
                new[] { Root + ".Group.Alpha", Root + ".Group.Beta" },
                Arch(Root).BoundedContextNamespaces.OrderBy(n => n, System.StringComparer.Ordinal));

        /// <summary>
        /// The cycle rules used to slice with a one-segment capture group, so two contexts grouped below an
        /// intermediate namespace produced no slices at all and their mutual dependency went unreported.
        /// They now slice by module root, so the cycle is found.
        /// </summary>
        [Fact]
        public void IsReported()
        {
            var ex = Assert.Throws<DcaRuleViolationException>(() => Rule("DCA-CYC-002", Root).Check(Arch(Root)));
            Assert.Contains("Cycle", ex.Message, System.StringComparison.Ordinal);
            Assert.Contains("Alpha", ex.Message, System.StringComparison.Ordinal);
            Assert.Contains("Beta", ex.Message, System.StringComparison.Ordinal);
        }
    }

    public sealed class ModuleRootOf
    {
        private const string Root = Fixtures + ".Grouped";

        [Fact]
        public void IsTheShortestPrefixOwningALayer() =>
            Assert.Equal(Root + ".Sales.Order", Arch(Root).ModuleRootOf(Root + ".Sales.Order.Adapter.Outgoing.Domain.Foo"));

        [Fact]
        public void IsNullWithoutALayerSegment() =>
            Assert.Null(Arch(Root).ModuleRootOf(Root + ".Infrastructure.Config"));

        [Fact]
        public void IsNullOutsideTheRoot() => Assert.Null(Arch(Root).ModuleRootOf("Elsewhere.Domain"));
    }

    /// <summary>No rule anywhere selects through the one-segment wildcard any more.</summary>
    [Fact]
    public void NoRuleUsesTheWildcardPatterns()
    {
        // Mechanical check over the rule sources: the parameterless wildcard properties are for tooling only.
        var rulesDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(
            System.AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DomainCentric.ArchRules", "Rules"));
        Assert.True(System.IO.Directory.Exists(rulesDir), "rule sources not found at " + rulesDir);
        var offenders = new List<string>();
        foreach (var file in System.IO.Directory.EnumerateFiles(rulesDir, "*.cs"))
        {
            foreach (var (line, index) in System.IO.File.ReadLines(file).Select((l, i) => (l, i + 1)))
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(
                        line, @"\bLayout\.(Domain|DomainModel|Application|SharedOutputPort|Adapter|IncomingAdapter|OutgoingAdapter)Pattern\b"))
                {
                    offenders.Add($"{System.IO.Path.GetFileName(file)}:{index}: {line.Trim()}");
                }
            }
        }

        Assert.Empty(offenders);
    }
}
