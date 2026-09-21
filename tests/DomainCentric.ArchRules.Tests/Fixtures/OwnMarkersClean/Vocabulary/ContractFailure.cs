using System;

namespace DomainCentric.ArchRules.Tests.Fixtures.OwnMarkersClean.Vocabulary;

/// <summary>A code base's own base class for a broken rule of the model.</summary>
public abstract class ContractFailure(string message) : Exception(message);
