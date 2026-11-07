using System;

namespace DomainCentric.BuildingBlocks.Application;

/// <summary>
/// Base class of every failure an input port's contract declares.
/// </summary>
/// <remarks>
/// <para>
/// <b>Who declares, raises and catches it.</b> The input port declares it as part of what its operation may answer;
/// the use case behind the port raises it; the incoming adapter that drives the port catches it and turns it into
/// its protocol's answer. Its one concrete branch is <see cref="UseCaseException"/>: every failure a use case
/// reports derives from that, so this type stays the classification in the hierarchy, not something a project
/// derives from directly.
/// </para>
/// <para>
/// <b>Never caught as such.</b> An incoming adapter that catches <c>InputPortException</c> answers every outcome the
/// same way; it catches the named subtypes, each with its own answer.
/// </para>
/// </remarks>
public abstract class InputPortException : PortException
{
    /// <summary>A failure described in the language of the input port's contract.</summary>
    protected InputPortException(string message)
        : base(message)
    {
    }

    /// <summary>A failure described in the language of the input port's contract, raised while handling another.</summary>
    protected InputPortException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
