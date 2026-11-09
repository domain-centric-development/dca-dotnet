using System;
using System.Collections.Generic;
using System.Linq;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;

namespace DomainCentric.ArchRules;

/// <summary>
/// One ArchUnitNET rule for a check the fluent API cannot state: a selection, and for each selected object
/// the violations found on it.
/// </summary>
/// <remarks>
/// Each selected object is evaluated; every violation found on it is one failed result, an object without
/// one is a passed result, and an empty selection yields no result at all — so the rule reports what it
/// selected the way a fluent rule does. It is what lets a hand-written check still be a single
/// <see cref="IArchRule"/>, the thing ArchUnitNET's <c>FreezingArchRule</c> builds a baseline from. A check
/// that runs several rules, one per bounded context, is not of this shape and stays a
/// <see cref="DcaRule.Check"/>.
/// </remarks>
internal static class SelectionArchRule
{
    /// <summary>A rule over the selected types, members or other named objects.</summary>
    public static IArchRule ForEach<T>(string description, Func<IEnumerable<T>> select, Func<T, IEnumerable<string>> violations)
        where T : IHasName =>
        new Rule<T>(description, select, t => t.FullName, violations);

    /// <summary>A rule over selected objects that are named by <paramref name="name"/>.</summary>
    public static IArchRule ForEach<T>(string description, Func<IEnumerable<T>> select, Func<T, string> name, Func<T, IEnumerable<string>> violations) =>
        new Rule<T>(description, select, name, violations);

    private sealed class Rule<T> : IArchRule
    {
        private readonly Func<IEnumerable<T>> _select;
        private readonly Func<T, string> _name;
        private readonly Func<T, IEnumerable<string>> _violations;

        internal Rule(string description, Func<IEnumerable<T>> select, Func<T, string> name, Func<T, IEnumerable<string>> violations)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
            _select = select ?? throw new ArgumentNullException(nameof(select));
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _violations = violations ?? throw new ArgumentNullException(nameof(violations));
        }

        public string Description { get; }

        public bool HasNoViolations(Architecture architecture) => Evaluate(architecture).All(result => result.Passed);

        public IEnumerable<EvaluationResult> Evaluate(Architecture architecture)
        {
            var results = new List<EvaluationResult>();
            foreach (var selected in _select().Select(s => (Object: s, Name: _name(s))).OrderBy(s => s.Name, StringComparer.Ordinal))
            {
                var identifier = new StringIdentifier(selected.Name);
                var lines = _violations(selected.Object).Distinct().ToList();
                if (lines.Count == 0)
                {
                    results.Add(new EvaluationResult(selected.Object, identifier, true, selected.Name + " passed", this, architecture));
                    continue;
                }

                results.AddRange(lines.Select(line => new EvaluationResult(selected.Object, identifier, false, line, this, architecture)));
            }

            return results;
        }

        public CombinedArchRuleDefinition And() => new(this, LogicalConjunctionDefinition.And);

        public CombinedArchRuleDefinition Or() => new(this, LogicalConjunctionDefinition.Or);

        public IArchRule And(IArchRule archRule) => new CombinedArchRule(this, LogicalConjunctionDefinition.And, archRule);

        public IArchRule Or(IArchRule archRule) => new CombinedArchRule(this, LogicalConjunctionDefinition.Or, archRule);

        public override string ToString() => Description;
    }
}
