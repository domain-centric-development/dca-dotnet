using System;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent.Conditions;

namespace DomainCentric.ArchRules.Rules;

/// <summary>
/// The fluent forms of <see cref="DcaArchitecture"/>'s placement predicates: a condition that a selected type
/// is placed in a layer, the counterpart of <c>ArchConditions.be(arch.inDomain())</c> in <c>dca-archunit</c>.
/// </summary>
internal static class LayerConditions
{
    /// <summary>A condition that holds for a type the predicate places, failing with "is not in &lt;where&gt;".</summary>
    public static Func<IType, ConditionResult> PlacedIn(Func<IType, bool> placed, string where) =>
        type => new ConditionResult(type, placed(type), $"{type.FullName} is not in {where}");
}
