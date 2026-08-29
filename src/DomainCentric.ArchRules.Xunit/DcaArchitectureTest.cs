using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DomainCentric.ArchRules.Xunit;

/// <summary>
/// xUnit base class that runs the whole DCA rule catalog as one theory case per rule, named by id:
/// <c>DCA-TAC-001</c>, <c>DCA-TAC-002</c>, …
/// </summary>
/// <example>
/// <code>
/// public sealed class ArchitectureTest : DcaArchitectureTest
/// {
///     protected override DcaLayout Layout => DcaLayout.ForRootNamespace("Acme.Shop");
///
///     protected override IEnumerable&lt;Assembly&gt; Assemblies => new[] { typeof(Program).Assembly };
/// }
/// </code>
/// Override <see cref="ExcludedRuleIds"/> to switch individual rules off, or <see cref="Rules"/> to pick
/// rule sets (<c>DcaRules.Only(Layout, "tactical", "hexagonal")</c>). Excluded rules pass without
/// checking anything.
/// </example>
public abstract class DcaArchitectureTest
{
    private static readonly object Gate = new();
    private static readonly Dictionary<Type, DcaArchitecture> Cache = new();

    /// <summary>The layout of the project under test.</summary>
    protected abstract DcaLayout Layout { get; }

    /// <summary>The production assemblies to import (one or more).</summary>
    protected abstract IEnumerable<System.Reflection.Assembly> Assemblies { get; }

    /// <summary>Identifiers of rules to skip. Default: none.</summary>
    protected virtual ISet<string> ExcludedRuleIds => new HashSet<string>();

    /// <summary>The rules to run. Default: the whole catalog minus <see cref="ExcludedRuleIds"/>.</summary>
    protected virtual IReadOnlyList<IDcaRule> Rules => DcaRules.AllExcept(Layout, ExcludedRuleIds);

    /// <summary>The imported architecture; loaded once per test class.</summary>
    protected DcaArchitecture Architecture
    {
        get
        {
            lock (Gate)
            {
                if (!Cache.TryGetValue(GetType(), out var arch))
                {
                    arch = DcaArchitecture.Load(Layout, Assemblies.ToArray());
                    Cache[GetType()] = arch;
                }

                return arch;
            }
        }
    }

    /// <summary>Every rule id of the catalog — the theory data.</summary>
    public static IEnumerable<object[]> RuleIds() => DcaRules.AllIds().Select(id => new object[] { id });

    [Theory]
    [MemberData(nameof(RuleIds))]
    public void DcaRule(string ruleId)
    {
        var rule = Rules.FirstOrDefault(r => r.Id == ruleId);
        if (rule is null)
        {
            return; // excluded or not selected
        }

        rule.Check(Architecture);
    }
}
