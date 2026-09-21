using System;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Roles.Container
{
    /// <summary>Stands in for a container stereotype the layout classifies into the container role.</summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class ClassifiedAttribute : Attribute
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Roles.Transport
{
    /// <summary>Stands in for a project's own attribute that fixes the protocol answer of the type it sits on; the layout classifies it into the transport-status role.</summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class FixesAnswerAttribute : Attribute
    {
    }
}
