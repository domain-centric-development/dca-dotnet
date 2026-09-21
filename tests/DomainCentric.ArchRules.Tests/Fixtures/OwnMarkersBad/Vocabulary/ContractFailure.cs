using System;

namespace DomainCentric.ArchRules.Tests.Fixtures.OwnMarkersBad.Vocabulary;

/// <summary>A code base's own base type for a broken rule of the model.</summary>
public abstract class ContractFailure : Exception
{
    protected ContractFailure(string message) : base(message)
    {
    }
}
