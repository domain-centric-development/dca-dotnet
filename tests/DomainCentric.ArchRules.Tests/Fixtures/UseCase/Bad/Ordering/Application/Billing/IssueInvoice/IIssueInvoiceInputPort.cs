namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.Billing.IssueInvoice;

/// <summary>DCA-USE-018: the input port of the Billing feature - qualified, because Application.IInputPort of this tree redeclares the base contract (DCA-USE-001).</summary>
public interface IIssueInvoiceInputPort : DomainCentric.BuildingBlocks.Hexagonal.Ports.In.IInputPort
{
}
