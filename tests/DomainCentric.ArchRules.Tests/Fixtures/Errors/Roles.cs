using System;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Roles.Container
{
    /// <summary>Stands in for a container stereotype the layout classifies into the container role.</summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class ClassifiedAttribute : Attribute
    {
    }
}
