using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DomainCentric.ArchRules.Tests.Fixtures.Errors.Results.Lending.Application.ReturnCopy;

public sealed record ReturnCopyCommand(string LoanId);

public sealed record ReturnCopyResult(string LoanId);

public interface IReturnCopyInputPort : IUseCase<ReturnCopyCommand, ReturnCopyResult>
{
}
