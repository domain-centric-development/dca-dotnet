namespace DomainCentric.BuildingBlocks.Application;

/// <summary>
/// Marker for a case of a use-case result that refuses the request.
/// </summary>
/// <remarks>
/// <para>
/// A use case may answer an expected refusal - the addressed thing does not exist, the caller may not act on it,
/// the model refused the input - in one of two styles, chosen per bounded context: it throws a named failure, or
/// it returns the refusal as a case of a closed result (a discriminated union whose cases its caller matches
/// over). Both are equally valid; within one context the style is uniform. This marker belongs to the second
/// style: every case of a closed result that refuses the request implements it, and a case that reports success
/// never does.
/// </para>
/// <para>
/// <b>A refusal rolls back like a thrown failure.</b> A use case that wrote something before it found out it has
/// to refuse must not commit that write. A thrown failure rolls the transaction back on its way out; a returned
/// value would not, so the transaction boundary looks at the value the work returns: when it is an
/// <see cref="IRefusal"/> - directly, or as the value a closed result currently holds - the boundary rolls back
/// and hands the result to the caller unchanged; see
/// <see cref="Transactions.ITransactionBoundary"/>. The rule for good code stays "decide, then write" - check
/// every refusal before the first write; the roll-back is the net that catches a use case that does not.
/// </para>
/// <para>
/// <b>Shape.</b> A refusal case carries the ids and figures the refusal compared, never display text; the
/// incoming adapter turns it into its protocol's answer, exactly as it would translate the thrown failure. The
/// marker declares no member: it says what a case is, the case's own properties say what happened.
/// </para>
/// <code>
/// public sealed record Reserved(SeatId Seat);
/// public sealed record SeatTaken(SeatId Seat) : IRefusal;
/// public sealed record ShowNotFound(ShowId Show) : IRefusal;
/// // the closed result: a union of Reserved | SeatTaken | ShowNotFound
/// </code>
/// <para>A closed result describes the outcomes a use case can answer with; some are refusals, some are not. One
/// whose outcomes are all successes - found or nothing found, sent or already sent - is a closed result all the same
/// and carries no <c>IRefusal</c>: marking such a case would roll back a success. The marker means "the request was
/// refused" whether or not a transaction is open - a read-only use case refuses too; the roll-back only acts where
/// something was written.</para>
/// </remarks>
public interface IRefusal
{
}
