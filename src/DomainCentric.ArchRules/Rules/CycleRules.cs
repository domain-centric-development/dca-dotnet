using System;
using System.Collections.Generic;
using ArchUnitNET.Fluent.Slices;

namespace DomainCentric.ArchRules.Rules;

/// <summary>
/// Namespace cycle detection: no circular dependencies between the per-context slices of one layer
/// (domain model, application, incoming adapters, outgoing adapters).
/// </summary>
/// <remarks>Reference: Clean Architecture, Acyclic Dependencies Principle (ADP).</remarks>
public sealed class CycleRules : IDcaRuleSet
{
    /// <summary>Java rules of this set that have no .NET counterpart (id → reason). None.</summary>
    public static readonly IReadOnlyDictionary<string, string> NotApplicable = new Dictionary<string, string>();

    public CycleRules(DcaLayout layout)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Rules = new IDcaRule[]
        {
            DomainPackagesFreeOfCycles(layout),
            ApplicationLayerFreeOfCycles(layout),
            OutgoingAdaptersFreeOfCycles(layout),
            IncomingAdaptersFreeOfCycles(layout),
        };
    }

    public string Name => "cycles";

    public IReadOnlyList<IDcaRule> Rules { get; }

    /// <summary>The layout this rule set was built for.</summary>
    public DcaLayout Layout { get; }

    public static IDcaRule DomainPackagesFreeOfCycles(DcaLayout layout) =>
        DcaRule.Of(
            "DCA-CYC-001",
            "Domain Namespaces must not have cyclic dependencies",
            "Domain model namespaces should have clear boundaries and no cycles (Acyclic Dependencies Principle)",
            arch => Slices(layout, $"{layout.DomainSegment}.Model"));

    public static IDcaRule ApplicationLayerFreeOfCycles(DcaLayout layout) =>
        DcaRule.Of(
            "DCA-CYC-002",
            "Application Layer must not have cyclic dependencies",
            "Application services should have clear boundaries and no cycles",
            arch => Slices(layout, layout.ApplicationSegment));

    public static IDcaRule OutgoingAdaptersFreeOfCycles(DcaLayout layout) =>
        DcaRule.Of(
            "DCA-CYC-003",
            "Outgoing Adapter Namespaces must not have cyclic dependencies",
            "Outgoing adapters should have clear boundaries and no cycles",
            arch => Slices(layout, $"{layout.AdapterSegment}.{layout.OutgoingSegment}"));

    public static IDcaRule IncomingAdaptersFreeOfCycles(DcaLayout layout) =>
        DcaRule.Of(
            "DCA-CYC-004",
            "Incoming Adapter Namespaces must not have cyclic dependencies",
            "Incoming adapters should have clear boundaries and no cycles",
            arch => Slices(layout, $"{layout.AdapterSegment}.{layout.IncomingSegment}"));

    /// <summary>
    /// One slice per context for the given layer: <c>Root.(*).Layer</c> and everything below it — the
    /// ArchUnitNET reading of ArchUnit's <c>Root.(*).layer..</c>.
    /// </summary>
    private static ArchUnitNET.Fluent.IArchRule Slices(DcaLayout layout, string layerSegments) =>
        SliceRuleDefinition.Slices()
            .Matching($"{layout.RootNamespace}.(*).{layerSegments}")
            .Should()
            .BeFreeOfCycles();
}
