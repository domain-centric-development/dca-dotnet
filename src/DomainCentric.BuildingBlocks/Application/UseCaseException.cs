using System;

namespace DomainCentric.BuildingBlocks.Application;

/// <summary>
/// Base class of every use-case failure the application layer reports to its callers.
/// </summary>
/// <remarks>
/// <para>
/// A use case fails for reasons the domain model cannot state on its own, because they are about the request
/// rather than about an invariant: the addressed aggregate does not exist, the caller may not act on it, or a
/// precondition on a second aggregate does not hold. Those failures belong to the application layer, which
/// knows the request, and they get a name that says which of them happened — one type per outcome, so a
/// caller can tell them apart without reading a message.
/// </para>
/// <para>
/// <b>Who declares, raises and catches it.</b> It is the failure of an input port's contract (see
/// <see cref="InputPortException"/>): the input port declares it, <em>only the use case</em> raises it, and the
/// incoming adapter catches each named subtype and answers it. An outgoing adapter never raises it - a failure an
/// output port declares is an <see cref="OutputPortException"/>, which the use case catches.
/// </para>
/// <para>
/// <b>Thrown or returned - per bounded context.</b> A use case answers its expected refusals (not found, not owned,
/// a precondition) in one of two styles, chosen per bounded context and uniform within it: it throws them as
/// subtypes of this class, or it returns them as cases of a closed result, each marked <see cref="IRefusal"/>. Both
/// styles are equally valid. In a context that returns results, not-found and not-owned are cases of the result,
/// not exceptions.
/// </para>
/// <para>
/// <b>What is not a use-case failure.</b> A broken rule of the model is a
/// <see cref="DomainCentric.BuildingBlocks.Ddd.Tactical.DomainException"/> and stays in the domain layer; an
/// argument guard stays the platform's own argument exception. A use-case failure is raised by the use case,
/// never by the model.
/// </para>
/// <para>
/// <b>Where it lives.</b> In the application layer, beside the use case that raises it. It carries no
/// metadata for container management or transport and names no transport vocabulary — the incoming adapter
/// maps each type onto the answer its protocol has for it, and it is the only layer that decides which answer
/// that is. Keeping the type free of that decision is what lets one use case serve several protocols at once.
/// </para>
/// <para>
/// <b>The name.</b> The layer's own word would be "application exception", but the platform occupies
/// <c>System.ApplicationException</c> and discourages deriving from it. Both libraries therefore say
/// <c>UseCaseException</c>, so one word names the concept in either stack.
/// </para>
/// </remarks>
public abstract class UseCaseException : InputPortException
{
    /// <summary>A failure described in the language of the use case.</summary>
    protected UseCaseException(string message)
        : base(message)
    {
    }

    /// <summary>A failure described in the language of the use case, raised while handling another failure.</summary>
    protected UseCaseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
