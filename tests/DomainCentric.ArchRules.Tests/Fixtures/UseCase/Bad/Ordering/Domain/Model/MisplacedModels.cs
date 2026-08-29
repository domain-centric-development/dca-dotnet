namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Domain.Model;

// DCA-USE-002: a Command in the domain
public sealed record ShipOrderCommand(OrderId OrderId);

// DCA-USE-003: a Query in the domain
public sealed record FindOrderQuery(OrderId OrderId);

// DCA-USE-006: a Result in the domain that is not a Value
public sealed record ShipOrderResult(OrderId OrderId);
