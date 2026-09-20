using System;

namespace DomainCentric.BuildingBlocks.Ddd.Tactical;

/// <summary>
/// Base class of every business-rule violation the domain model raises.
/// </summary>
/// <remarks>
/// <para>
/// A domain exception has a name a domain expert would recognise: it says which rule of the model was
/// broken, not which technical operation failed. The test is the name — if the failure has a word in the
/// Ubiquitous Language, it is a domain exception with that word in its name; if it has none, it is not one.
/// </para>
/// <para>
/// <b>What is not a domain exception.</b> An argument guard is a programming-error contract, not a business
/// rule: a null check, a range check or a "must not be blank" check in a constructor states what a caller
/// must never pass, and the platform's own argument exception remains the correct answer there. Derive from
/// this type only for a rule the model itself owns — a forbidden state transition, an invariant across
/// several properties, a quantity the aggregate refuses.
/// </para>
/// <para>
/// <b>Where it lives.</b> In the domain layer, beside the model that raises it. It carries no metadata for
/// container management, persistence or transport, and it names no transport vocabulary — no status code, no
/// response shape. Translating a domain exception into a protocol answer is the incoming adapter's work, and
/// the adapter is the only layer that knows the protocol.
/// </para>
/// <para>
/// See <c>DomainCentric.BuildingBlocks.Application.UseCaseException</c> for the failures a use case reports.
/// </para>
/// </remarks>
public abstract class DomainException : Exception
{
    /// <summary>A violation described in the language of the model.</summary>
    protected DomainException(string message)
        : base(message)
    {
    }

    /// <summary>A violation described in the language of the model, raised while handling another failure.</summary>
    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
