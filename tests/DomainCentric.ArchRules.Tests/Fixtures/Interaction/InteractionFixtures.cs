using DomainCentric.BuildingBlocks.Ddd.Strategic; using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;
namespace DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Good.Consumer { [BoundedContext("Consumer")][Upstream("U",Translation.AntiCorruptionLayer,Consumes.Api)][Upstream("V",Translation.AntiCorruptionLayer,Consumes.Api)] public class Context {} }
namespace DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Good.U { [BoundedContext("U")] public class Context {} }
namespace DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Good.U.Api { public interface IContract {} }
namespace DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Good.V { [BoundedContext("V")] public class Context {} }
namespace DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Good.V.Api { public interface IContract {} }
namespace DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Good.Consumer.Domain.Model {public class Local {}}
namespace DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Good.Consumer.Adapter.Outgoing.Translation {public class UTranslator {private DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Good.U.Api.IContract contract=null!;private DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Good.Consumer.Domain.Model.Local local=null!;}}
namespace DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Good.Consumer.Adapter.Outgoing.Translation {public class VTranslator {private DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Good.V.Api.IContract contract=null!;private DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Good.Consumer.Domain.Model.Local local=null!;}}
namespace DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Bad.Consumer { [BoundedContext("Consumer")][Upstream("U",Translation.AntiCorruptionLayer,Consumes.Api)][Upstream("V",Translation.AntiCorruptionLayer,Consumes.Api)] public class Context {} }
namespace DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Bad.U { [BoundedContext("U")] public class Context {} }
namespace DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Bad.U.Api { public interface IContract {} }
namespace DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Bad.V { [BoundedContext("V")] public class Context {} }
namespace DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Bad.V.Api { public interface IContract {} }
namespace DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Bad.Consumer.Domain.Model {public class Local {}}
namespace DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Bad.Consumer.Adapter.Outgoing.Translation {public class UTranslator {private DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Bad.U.Api.IContract contract=null!;private DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Bad.Consumer.Domain.Model.Local local=null!;}}
namespace DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Bad.Consumer.Adapter.Outgoing.Translation {public class VTranslator {private DomainCentric.ArchRules.Tests.Fixtures.AclEvidence.Bad.V.Api.IContract contract=null!;}}