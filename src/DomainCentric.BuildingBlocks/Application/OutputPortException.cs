using System;

namespace DomainCentric.BuildingBlocks.Application;

/// <summary>
/// Base class of every failure an output port's contract declares.
/// </summary>
/// <remarks>
/// <para>
/// <b>Who declares, raises and catches it.</b> The output port declares it in its contract, in the application
/// layer beside the port; the outgoing adapter that implements the port raises it, because the contract asks for
/// it; the use case that calls the port catches it and decides the answer - a case of its result where the caller
/// can answer it, or its own <see cref="UseCaseException"/>. The adapter raises it, but does not own it: an adapter
/// never invents a failure the application has not declared.
/// </para>
/// <para>
/// <b>For an expected failure the port's caller can answer</b> - a uniqueness only the store can see, or a foreign
/// system behind the port that refused or did not answer (a payment provider, a mail provider: an external system on
/// the context map). A failure of the system's own infrastructure - its database, its broker - is no part of the
/// contract and stays the platform's own exception.
/// </para>
/// <para>
/// <b>Inside the transaction.</b> The use case may catch it inside its transaction and return a refusal: a returned
/// <see cref="IRefusal"/> rolls the transaction back like the exception would have.
/// </para>
/// <para>
/// <b>Never caught as such.</b> A use case catches the named subtype the port declares, not this base.
/// </para>
/// </remarks>
public abstract class OutputPortException : PortException
{
    /// <summary>A failure described in the language of the output port's contract.</summary>
    protected OutputPortException(string message)
        : base(message)
    {
    }

    /// <summary>A failure described in the language of the output port's contract, raised while handling another - typically the platform's own failure the adapter translated.</summary>
    protected OutputPortException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
