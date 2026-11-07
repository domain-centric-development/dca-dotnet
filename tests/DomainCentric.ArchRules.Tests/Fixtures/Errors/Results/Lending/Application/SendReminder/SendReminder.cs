using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;
using OneOf;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Results.Lending.Application.SendReminder;

public sealed record SendReminderCommand(string LoanId);

/// <summary>
/// A closed result whose outcomes are both successes: sent now, or sent before. Neither refuses the request, so
/// neither carries the refusal marker.
/// </summary>
public sealed class SendReminderResult : OneOfBase<SendReminderResult.Sent, SendReminderResult.AlreadySent>
{
    public SendReminderResult(Sent sent)
        : base(sent)
    {
    }

    public SendReminderResult(AlreadySent alreadySent)
        : base(alreadySent)
    {
    }

    public sealed record Sent(string LoanId);

    public sealed record AlreadySent(string LoanId);
}

public interface ISendReminderInputPort : IInputPort
{
    SendReminderResult Send(SendReminderCommand command);
}
