using DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Domain.Model;

namespace DomainCentric.ArchRules.Tests.Fixtures.UseCase.Bad.Ordering.Application.Shared;

// DCA-USE-015: a part record shared between use cases in Application.Shared hides an entity
public sealed record OrderPart(OrderLine Line, int Position);
