using System.Collections.Generic;

namespace DomainCentric.ArchRules;

/// <summary>
/// A named group of <see cref="IDcaRule"/>s, e.g. all tactical DDD rules. Rule sets are stateless with
/// respect to the code under test — they receive the <see cref="DcaArchitecture"/> at check time.
/// </summary>
public interface IDcaRuleSet
{
    /// <summary>Short name, e.g. <c>tactical</c>. Also the identifier prefix segment in lower case.</summary>
    string Name { get; }

    /// <summary>The rules of this set, in catalog order.</summary>
    IReadOnlyList<IDcaRule> Rules { get; }
}
