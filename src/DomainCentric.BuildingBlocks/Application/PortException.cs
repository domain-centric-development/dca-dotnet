using System;

namespace DomainCentric.BuildingBlocks.Application;

/// <summary>
/// Base class of every failure a port's contract declares.
/// </summary>
/// <remarks>
/// <para>
/// A port is a contract between the application and the outside: an input port between the application and the
/// adapter that drives it, an output port between the application and the adapter it drives. A failure that
/// belongs to such a contract is declared by the port, raised on one side of it and caught on the other. Which side
/// is which depends on the direction, so the two kinds have their own bases: <see cref="InputPortException"/> -
/// declared by an input port, raised by the use case, caught by the incoming adapter - and
/// <see cref="OutputPortException"/> - declared by an output port, raised by the adapter that implements it, caught
/// by the use case.
/// </para>
/// <para>
/// <b>Abstract on purpose, and never caught as such.</b> No code raises a <c>PortException</c> directly, and a
/// caller that catches it, or one of its two direct subclasses, answers every outcome the same way - the named
/// subtypes exist so that each outcome gets its own answer.
/// </para>
/// <para>
/// <b>What is not a port failure.</b> A broken rule of the model is a
/// <see cref="DomainCentric.BuildingBlocks.Ddd.Tactical.DomainException"/>; an argument guard stays the platform's own
/// argument exception; a failure of the infrastructure - a connection lost, a timeout - is the platform's own
/// exception and travels to the boundary that can answer it.
/// </para>
/// <para>
/// <b>Where it lives.</b> In the application layer, beside the port that declares it. It carries no metadata for
/// container management or transport and names no transport vocabulary.
/// </para>
/// </remarks>
public abstract class PortException : Exception
{
    /// <summary>A failure described in the language of the port's contract.</summary>
    protected PortException(string message)
        : base(message)
    {
    }

    /// <summary>A failure described in the language of the port's contract, raised while handling another.</summary>
    protected PortException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
